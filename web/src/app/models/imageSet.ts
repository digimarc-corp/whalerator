import { Platform } from './platform';
import { Image } from './image';

export class ImageSet {
    public platforms: Platform[];
    public date: Date;
    public setDigest: String;
    public tags: String[];
    public images: Image[];
}
