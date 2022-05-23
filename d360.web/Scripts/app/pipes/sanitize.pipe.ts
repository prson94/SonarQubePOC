import { Pipe, PipeTransform, SecurityContext } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';

@Pipe({ name: 'sanitize' })
export class SanitizePipe implements PipeTransform {
    constructor(private sanitizer: DomSanitizer) { }
    transform(value: string): any {
        return this.sanitizer.sanitize(SecurityContext.HTML, value);
    }
}