import { AfterViewChecked, Directive, ElementRef, HostListener, Input } from '@angular/core';

@Directive({
    selector: '[d3s-text-highlight]'
})
export class TextHighlightDirective implements AfterViewChecked  {
   
    
    @Input("d3s-text-highlight")
    private isHighlight: boolean = false;

    constructor(private el: ElementRef) {
    }

    @HostListener('click', ['$event.target'])
    onClick($event) {
        this.isHighlight = false;
    }
    ngAfterViewChecked(): void {
        if (this.isHighlight) {
            this.el.nativeElement.focus();
            this.el.nativeElement.select();
        }
    }

}