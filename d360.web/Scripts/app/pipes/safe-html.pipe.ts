import { Pipe, PipeTransform, SecurityContext } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';


@Pipe({ name: 'safeHtml' })
export class SafeHtmlPipe implements PipeTransform {
    constructor(private sanitized:DomSanitizer) {}
    transform(value: string): any {
        if (!value) return "";
        return this.sanitized.sanitize(SecurityContext.HTML, value);
    }
}