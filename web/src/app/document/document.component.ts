import { Component, OnInit, Input } from '@angular/core';
import { ImageSet } from '../models/imageSet';
import { Platform } from '../models/platform';
import { CatalogService } from '../catalog.service';
import { Image } from '../models/image';
import { Document } from '../models/document';
import { History } from '../models/history';
import { isError } from '../web-service';

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
    if (image && !image.documents) {
      this.getDocuments(image);
    } else if (image && image.documents.length > 0) {
      this.selected = image.documents[0];
    }
  }
  get image(): Image { return this._image; }

  selected: Document | History;

  constructor(private catalog: CatalogService) { }

  ngOnInit() {
  }

  isHistory(obj: any): Boolean {
    return obj instanceof History;
  }

  isDocument(obj: any): Boolean {
    return obj instanceof Document;
  }

  selectHistory() {
    this.selected = new History();
  }

  select(document: Document) {
    this.selected = document;
  }

  getDocuments(image: Image, next?: () => void) {
    const filename = 'readme.md';
    this.catalog.getFile(this.repository, image.digest, filename).subscribe(r => {
      if (isError(r)) {
        this.image.documents = [];
        console.log(`Could not find a file ${filename}`);
        if (!this.selected) {
          this.selectHistory();
        }
      } else {
        const document = new Document();
        document.name = filename;
        document.content = r.toString();
        if (!this.image.documents) {
          this.image.documents = [document];
          if (!this.selected) {
            this.selected = document;
          }
        } else {
          this.image.documents.push(document);
        }
        const stub = new Document();
        stub.content = '## Fuck off';
        stub.name = 'relnotes';
        this.image.documents.push(stub);
      }
    });
  }
}
