import { Platform } from './platform';
import { Document } from './document';
import { History } from './history';

export class Image {
    public digest: String;
    public platform: Platform;
    public history: History[];

    public documents: Document[];
}
