import { Pipe, PipeTransform } from '@angular/core';
import { Image } from './models/image';

@Pipe({
  name: 'tagSort'
})
export class TagSortPipe implements PipeTransform {

  transform(value: Image[]): any {
    return value.reverse();
  }

}
