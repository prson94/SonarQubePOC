import { Directive, ElementRef, Input, AfterViewChecked } from '@angular/core';

@Directive({
    selector: '[d3s-text-highlight]'
})
export class TextHighlightDirective implements AfterViewChecked  {
   
    
    @Input("d3s-text-highlight")
    private isHighlight: boolean = false;

    constructor(private el: ElementRef) {
    }


    ngAfterViewChecked(): void {
        if (this.isHighlight) {
            this.el.nativeElement.focus();
            this.el.nativeElement.select();
        }
    }

}