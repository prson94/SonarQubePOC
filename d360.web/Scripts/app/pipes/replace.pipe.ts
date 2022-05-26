import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'replaceString' })
export class ReplaceStringPipe implements PipeTransform {
    transform(value: string, stringToReplace: string, replacementString: string): string {

        if (!value || !stringToReplace || !replacementString) {
            return value;
        }

        // eslint-disable-next-line
        return value.replace(new RegExp(stringToReplace, 'g'), replacementString);
    }
}