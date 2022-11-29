import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'iconName' })
export class IconNamePipe implements PipeTransform {
    transform(iconClass: string): string {
        return iconClass.split('-').slice(1).map((word) => {
            return `${word[0].toUpperCase()}${word.slice(1)}`;
        }).join(" ");
    }
}
