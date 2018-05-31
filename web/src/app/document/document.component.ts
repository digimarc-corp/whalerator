import { Component, OnInit, Input } from '@angular/core';
import { ImageSet } from '../models/imageSet';
import { Platform } from '../models/platform';
import { CatalogService } from '../catalog.service';
import { Image } from '../models/image';
import { Document } from '../models/document';
import { History } from '../models/history';
import { isError } from '../web-service';
import { ConfigService } from '../config.service';

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
      this.getDocuments();
    } else if (image && image.documents.length > 0) {
      this.selected = image.documents[0];
    }
  }
  get image(): Image { return this._image; }

  selected: Document | History[];

  constructor(private catalog: CatalogService,
    private config: ConfigService) { }

  ngOnInit() {
  }

  isHistory(obj: any): Boolean {
    return obj && obj[0] instanceof History;
  }

  isDocument(obj: any): Boolean {
    return obj instanceof Document;
  }

  selectHistory() {
    this.selected = this.image.history;
  }

  select(document: Document) {
    this.selected = document;
  }

  getDocuments() {
    const lists = this.config.config.searchLists.slice().reverse().map(l => l.slice().reverse());
    if (lists.length > 0) {
      this.getDocumentLists(lists);
    } else {
      this.image.documents = [];
    }
  }

  getDocumentLists(lists: String[][]) {
    if (lists.length > 0) {
      this.getDocumentStack(lists.pop(), () => {
        this.getDocumentLists(lists);
      });
    }
  }

  getDocumentStack(stack: String[], next?: () => void) {
    // console.log(`Searching for hit from '${stack}'`);
    if (stack.length > 0) {
      this.getDocument(stack.pop(), (success) => {
        if (!success) {
          this.getDocumentStack(stack, next);
        } else if (next) {
          next();
        }
      });
    } else {
      if (next) {
        next();
      }
    }
  }

  getDocument(filename: String, next?: (success: Boolean) => void) {
    // console.log(`Searching for ${filename}`);
    this.catalog.getFile(this.repository, this.image.digest, filename).subscribe(r => {
      if (isError(r)) {
        if (next) {
          next(false);
        } else {
          this.image.documents = [];
          if (!this.selected) {
            this.selectHistory();
          }
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
        next(true);
      }
    });
  }
}
