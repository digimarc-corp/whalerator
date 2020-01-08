/*
   Copyright 2018 Digimarc, Inc

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.

   SPDX-License-Identifier: Apache-2.0
*/

import { Component, OnInit, Input, ChangeDetectorRef } from '@angular/core';
import { ImageSet } from '../models/imageSet';
import { Platform } from '../models/platform';
import { CatalogService } from '../catalog.service';
import { Image } from '../models/image';
import { Document } from '../models/document';
import { History } from '../models/history';
import { isError, isHttpString } from '../web-service';
import { ConfigService } from '../config.service';
import { ScanResult } from '../models/scanResult';
import { Observable, config, Subscriber } from 'rxjs';
import { delay } from 'q';
import { Console } from '@angular/core/src/console';

@Component({
  selector: 'app-document',
  templateUrl: './document.component.html',
  styleUrls: ['./document.component.scss']
})
export class DocumentComponent implements OnInit {

  @Input() repository: String;

  _image: Image;
  @Input()
  set image(image: Image) {
    this._image = image;
    this.selected = null;
    if (image && !image.scanResult && this.config.config.secScanner) {
      this.getScan();
    }
    if (this.config.config.docScanner) {
      if (image && !image.documents) {
        this.searchStatus = 'Searching';
        this.getDocuments();
      } else if (image && image.documents.length > 0) {
        this.selected = image.documents[0];
        this.searchStatus = null;
      }
    } else {
      this.selected = image.history;
    }
  }
  get image(): Image { return this._image; }

  selected: Document | History[] | ScanResult;
  searching: Observable<string>[] = [];
  searchStatus: String;

  constructor(private catalog: CatalogService,
    private config: ConfigService, private changeDetector: ChangeDetectorRef) { }

  ngOnInit() {
  }

  getScan() {
    this.catalog.getScan(this.repository, this.image.digest).subscribe(r => {
      if (isError(r)) {
        console.error("Failed to get scan result.");
      } else {
        if (r.digest == this.image.digest) {
          this.image.scanResult = new ScanResult(r);
          if (r.status == "Pending") {
            delay(5000).then(() => {
              this.getScan();
            });
          }
          this.changeDetector.detectChanges();
        }
      }
    })
  }

  getScanHeadline(): String {
    if (this.image.scanResult.status == "Succeeded") {
      let components = this.image.scanResult.vulnerableComponents == null ?
        0 : this.image.scanResult.vulnerableComponents.length;
      return components == 0 ? "No known issues" : components == 1 ? "1 known issue" : `${components} known issues`;
    } else if (this.image.scanResult.status == "Pending") {
      return "Scan pending";
    } else {
      return "Scan failed";
    }
  }

  scanningEnabled(): Boolean {
    return this.config.config.secScanner;
  }

  docsEnabled(): Boolean {
    return this.config.config.docScanner;
  }

  isHistory(obj: any): Boolean {
    return obj && obj[0] instanceof History;
  }

  isDocument(obj: any): Boolean {
    return obj instanceof Document;
  }

  isScan(obj: any): Boolean {
    return obj instanceof ScanResult;
  }

  selectHistory() {
    this.selected = this.image.history;
  }

  selectScan() {
    this.selected = this.image.scanResult;
  }

  select(document: Document) {
    this.selected = document;
    // once there are more than 2-3 documents in the image, it seems to break change detection
    // and we have to use a hack here and below actually make it work...
    // needs further investigation
    this.changeDetector.detectChanges();
  }

  pushDocument(document: Document) {
    if (this.image.documents) {
      if (!this.image.documents.some((d) => d.name === document.name)) {
        this.image.documents.push(document);
      }
    } else {
      this.image.documents = [document];
      if (!this.selected) {
        this.selected = document;
      }
    }
    this.changeDetector.detectChanges();
  }

  // translate search lists into stacks of documents to search for
  getDocuments() {
    if (this.config.config.searchLists.length > 0) {
      // start a searcher observable for each potential stack of documents
      this.config.config.searchLists.forEach(d => {
        const obv = new Observable<string>((o) => {
          this.searchStack(d.map(s => s.toString()), o);
        });
        this.addSearch(obv);
        obv.subscribe(r => console.log(r), err => console.error(err), () => this.removeSearch(obv));
      });

      // start rotating the search status mussage
      this.rotateSearchStatus(0);

    } else {
      this.image.documents = [];
    }
  }

  addSearch(o: Observable<string>) {
    this.searching.push(o);
  }

  removeSearch(o: Observable<string>) {
    this.remove(this.searching, o);
    if (this.searching.length === 0 && !this.image.documents) {
      this.searchStatus = 'No docs found';
      if (!this.selected) {
        this.selected = this.image.history;
      }
      this.changeDetector.detectChanges();
    }
  }

  remove<T>(list: T[], item: T) {
    const index = list.indexOf(item, 0);
    if (index > -1) {
      list.splice(index, 1);
    }
  }

  removeAll<T>(list: T[]) {
    list.splice(0, list.length);
  }

  rotateSearchStatus(spin: number) {
    if (this.searching.length > 0) {
      this.searchStatus = 'Searching' + '.'.repeat(spin++ % 4);
      delay(500).then(() => this.rotateSearchStatus(spin));
    } else {
      if (this.image.documents) {
        this.searchStatus = null;
      } else {
        this.searchStatus = 'No docs found';
      }
    }
    this.changeDetector.detectChanges();
  }

  // begin searching for a hit from a list of documents
  searchStack(list: string[], s: Subscriber<string>) {
    if (list.length > 0) {
      this.pollDocument(list);
      delay(1000).then(() => {
        this.searchStack(list, s);
      });
    } else {
      s.complete();
    }
  }

  pollDocument(list: string[]) {
    const filename = list[0];
    console.log(`Searching for ${filename}`);
    const digest = this.image.digest;
    this.catalog.getFile(this.repository, this.image.digest, filename).subscribe(r => {
      if (isError(r)) {
        // if this document doesn't exist or is otherwise unretrievable, nix it and move on
        this.remove(list, filename);
      } else if (isHttpString(r)) {
        if (r.status === 200) {
          // verify we didn't have a race and already load a document from this list, or switch images
          if (list.length > 0 && this.image.digest === digest) {
            const document = new Document();
            document.name = filename;
            document.content = r.body.toString();
            this.pushDocument(document);
          } else {
            console.log("Mismatched digests, discarding document load");
          }
          this.removeAll(list);
        } else if (r.status === 202) {
          console.log(`Search pending for ${filename}`);
        } else {
          console.error(`Unexpected service response ${r.status}`);
        }
      } else {
        console.log('WTF?');
      }
    });
  }
}
