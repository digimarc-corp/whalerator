import { Platform } from './platform';
import { Document } from './document';

export class Image {
    public digest: String;
    public platform: Platform;

    public documents: Document[];
}
