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

import { Component, OnInit, Input, ChangeDetectorRef, ViewChild } from '@angular/core';
import { ImageSet } from '../models/image-set';
import { Platform } from '../models/platform';
import { CatalogService } from '../catalog.service';
import { Image } from '../models/image';
import { Document } from '../models/document';
import { History } from '../models/history';
import { isError, isHttpString } from '../web-service';
import { ConfigService } from '../config.service';
import { ScanResult } from '../models/scan-result';
import { Observable, Subscriber } from 'rxjs';
import { delay } from 'q';
import { FileListing } from '../models/file-listing';
import { HttpResponse } from '@angular/common/http';
import { stringify } from 'querystring';
import { MarkdownComponent } from 'ngx-markdown';

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

  public onMarkdownLoad(event: string) {
    const element = document.getElementById('mdViewer');
    if (element) {
      this.reEl(element);
    }
  }

  public markdownClick(image: Image) {
    const a = event.target as HTMLAnchorElement;
    if (a) {
      const src = new URL(a.href);
      // is this link relative?
      if (src.hostname === window.location.hostname) {        
        const path = src.pathname.replace(/^\//, '');
        // does it have a path component?
        if (path) {
          const digest = this.getLayerDigestForPath(path, image.files);
          // can we find it in the index of known files?
          if (!digest) {
            if (!a.classList.contains('deadlink')) {
              a.classList.add('deadlink');
              a.insertAdjacentHTML('afterend',
                '<img title="Dead link" src="assets/exclamation-outline.svg" class="action-icon deadlink" />');
            }
          } else {
            // does it look like a markdown?
            if (/\.md$/.test(src.pathname)) {
              const doc = image.documents.find(d => d.name === path);
              if (doc) {
                this.select(doc);
              } else {
                this.loadDocument(image, digest, path, true);
              }
            } else {
              // it's something else, open in new window/tab
              const svc = new URL(this.configService.config.serviceHost);
              svc.pathname = `api/repository/${this.repository}/${digest}/file`;
              svc.search = `?${path}`;
              window.open(svc.toString());
            }
          }
        } else {
          // no path, so effectively they're asking to reopen root. Probably a link to 'localhost'
          window.open(window.location.origin);
        }
      } else {
        // external link, open externally
        window.open(src.toString());
      }
      return false;
    }
  }

  reEl(element: Element) {
    if (element.tagName.toLowerCase() === 'a') {
      this.retargetAnchor(element);
    } else if (element.tagName.toLowerCase() === 'img') {
      this.retargetImg(element as HTMLImageElement);
    } else {
      let i: number;
      for (i = 0; i < element.children.length; i++) {
        this.reEl(element.children[i]);
      }
    }
  }

  private retargetAnchor(element: Element) {
    const a = element as HTMLAnchorElement;
    a.onclick = () => this.markdownClick(this.image);
  }

  private retargetImg(img: HTMLImageElement) {
    if (img) {
      const src = new URL(img.src);
      if (src.hostname === window.location.hostname) {
        const path = src.pathname.replace(/^\//, '');
        const digest = this.getLayerDigestForPath(path, this.image.files);
        const svc = new URL(this.configService.config.serviceHost);
        src.host = svc.host;
        src.pathname = `/api/repository/${this.repository}/file/${digest}`;
        src.search = `?path=${path}`;
        img.src = src.toString();
      }
    }
  }

  getLayerDigestForPath(path: string, index: FileListing[]): string {
    const file = index.map(l => l.files.map(f => ({ layer: l.digest, path: f })))
      .flat()
      .find(f => f.path === path);
    return file ? file.layer : null;
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
    const targets = this.configService.config.searchLists.flat().join(';');
    this.catalog.getFileList(this.repository, this.image.digest, targets).subscribe(r => {
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
      const files = image.files.map(l => l.files.map(f => ({ layer: l.digest, path: f }))).flat();
      const matches = files.filter(f => this.configService.config.searchLists.flat().some(s => s.toLowerCase() === f.path.toLowerCase()));
      if (matches.length > 0) {
        matches.map(m => this.loadDocument(image, m.layer, m.path));
      } else {
        this.selectHistory();
      }
    }
  }

  loadDocument(image: Image, layer: string, path: string, focus?: boolean) {
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
          if (this.image === image && focus) {
            this.select(document);
          }
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
