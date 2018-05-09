import { Pipe, PipeTransform } from '@angular/core';
import { ImageSet } from './models/imageSet';

@Pipe({
  name: 'tagSort'
})
export class TagSortPipe implements PipeTransform {

  transform(value: ImageSet[]): any {
    return value.reverse();
  }
}
