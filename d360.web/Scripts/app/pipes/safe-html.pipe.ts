import { Pipe, PipeTransform, Injectable } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';


@Pipe({ name: 'safeHtml' })
export class SafeHtmlPipe implements PipeTransform {
    constructor(private sanitized:DomSanitizer) {}
    transform(value: string): any {
        let chkScript = new RegExp("<[^>]*script");
        if (chkScript.test(value)) return "";
          
           return this.sanitized.bypassSecurityTrustHtml(value);
    }
}