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
import { Observable, Subscriber } from 'rxjs';
import { delay } from 'q';
import { FileListing } from '../models/fileListing';
import { HttpResponse } from '@angular/common/http';

@Component({
  selector: 'app-document',
  templateUrl: './document.component.html',
  styleUrls: ['./document.component.scss']
})
export class DocumentComponent implements OnInit {

  @Input() repository: string;

  _image: Image;
  @Input()
  set image(image: Image) {
    this._image = image;
    this.selected = null;
    if (image && !image.scanResult && this.configService.config.secScanner) {
      this.getScan();
    }
    if (this.configService.config.docScanner) {
      if (image && !image.documents) {
        this.searchStatus = 'Searching';
        this.rotateSearchStatus(0);
        this.getFileListing(image);
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
  searchStatus: string;

  constructor(private catalog: CatalogService,
    private configService: ConfigService,
    private changeDetector: ChangeDetectorRef) { }

  ngOnInit() {
  }

  getScan() {
    this.catalog.getScan(this.repository, this.image.digest).subscribe(r => {
      if (isError(r)) {
        console.error('Failed to get scan result.');
      } else {
        if (r.digest === this.image.digest) {
          this.image.scanResult = new ScanResult(r);
          if (r.status === 'Pending') {
            delay(5000).then(() => {
              this.getScan();
            });
          }
          this.changeDetector.detectChanges();
        }
      }
    });
  }

  getScanHeadline(): String {
    if (this.image.scanResult.status === 'Succeeded') {
      const components = this.image.scanResult.vulnerableComponents == null ?
        0 : this.image.scanResult.vulnerableComponents.length;
      return components === 0 ? 'No known issues' : components === 1 ? '1 known issue' : `${components} known issues`;
    } else if (this.image.scanResult.status === 'Pending') {
      return 'Scan pending';
    } else {
      return 'Scan failed';
    }
  }

  scanningEnabled(): boolean {
    return this.configService.config.secScanner;
  }

  docsEnabled(): boolean {
    return this.configService.config.docScanner;
  }

  isHistory(obj: any): boolean {
    return obj && obj[0] instanceof History;
  }

  isDocument(obj: any): boolean {
    return obj instanceof Document;
  }

  isScan(obj: any): boolean {
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

  pushDocument(image: Image, document: Document) {
    if (image.documents) {
      if (!image.documents.some((d) => d.name === document.name)) {
        image.documents.push(document);
      }
    } else {
      image.documents = [document];
      if (this.image === image && !this.selected) {
        this.selected = document;
      }
    }
    this.rotateSearchStatus(0);
    this.changeDetector.detectChanges();
  }

  getFileListing(image: Image) {
    console.log(`Requesting file index`);
    this.catalog.getFileList(this.repository, this.image.digest, null).subscribe(r => {
      if (isError(r)) {
        // indexing failed for some reason.
        console.error('Unable to get file index from service.');
        this.image.files = new FileListing[0];
      } else if (r instanceof HttpResponse) {
        r = r as HttpResponse<FileListing[]>;
        if (r.status === 200) {
          image.files = r.body;
          this.searchStatus = null;
          this.loadDocuments(image);
        } else if (r.status === 202) {
          delay(1000).then(() => this.getFileListing(image));
        } else {
          console.error('Indexing failed');
        }
      }
    });
  }

  loadDocuments(image: Image) {
    if (this.configService.config.searchLists.length > 0) {
      const files = image.files.map(l => l.files.map(f => ({ layer: l.digest, path: f}) )).flat();
      const matches = files.filter(f => this.configService.config.searchLists.flat().some(s => s.toLowerCase() === f.path.toLowerCase()));
      matches.map(m => this.loadDocument(image, m.layer, m.path));
    }
  }

  loadDocument(image: Image, layer: string, path: string) {
    const document = new Document();
    document.name = path;
    document.content = `*Loading ${path}...*\n\n\n\n\n \`${layer}\``;

    this.pushDocument(image, document);
    this.catalog.getFile(this.repository, layer, path).subscribe(r => {
      if (isError(r)) {
        console.error(`Couldn't load ${path}`);
      } else if (isHttpString(r)) {
        if (r.status === 200) {
          document.content = r.body;
        }
      }
    });
  }

  rotateSearchStatus(spin: number) {
    if (!this.image.files) {
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
}
