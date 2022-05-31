import { NgModule, Directive, ElementRef, AfterViewInit, Input } from '@angular/core';
import { DomHandler } from 'primeng/dom';
import { CommonModule } from '@angular/common';

@Directive({
    selector: '[igTextArea]'

})
export class TextAreaDirective implements AfterViewInit {

    @Input() required: boolean;

    @Input() disabled: boolean;

    constructor(public el: ElementRef) { }

    ngAfterViewInit() {
        DomHandler.addMultipleClasses(this.el.nativeElement, this.getStyleClass());

        this.required = this.el.nativeElement.getAttribute("required");
        this.disabled = this.el.nativeElement.getAttribute("disabled");

        if (this.required == null) {
            this.el.nativeElement.setAttribute("placeholder", $localize`Optional`);
        } else {
            this.el.nativeElement.setAttribute("placeholder", $localize`Value required`);
            this.el.nativeElement.setAttribute("aria-required", true);

        }
    }

    getStyleClass(): string {
        return 'ig-textarea';
    }
}

@NgModule({
    imports: [CommonModule],
    exports: [TextAreaDirective],
    declarations: [TextAreaDirective]
})
export class TextAreaModule { }