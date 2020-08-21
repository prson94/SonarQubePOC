import { NgModule, Directive, ElementRef, AfterViewInit, OnDestroy, Input, forwardRef, Provider } from '@angular/core';
import { DomHandler } from 'primeng/dom';
import { CommonModule } from '@angular/common';
import { NG_VALIDATORS, AbstractControl } from '@angular/forms';

@Directive({
    selector: '[igInput]'

})
export class InputDirective implements AfterViewInit, OnDestroy {

    @Input() tooltip: string;
    public _label: string;
    public _istextarea: boolean;
    private control: AbstractControl;
    constructor(public el: ElementRef) { }

    ngAfterViewInit() {
        DomHandler.addMultipleClasses(this.el.nativeElement, this.getStyleClass());
        if (this.tooltip) {
            this.el.nativeElement.setAttribute("title", this.tooltip);
            this.el.nativeElement.setAttribute("aria-label", this.tooltip);
        }
    }

    getStyleClass(): string {
        return 'ig-input';
    }

    @Input() get label(): string {
        return this._label;
    }
    set label(val: string) {
        this._label = val;

        let labelElement = DomHandler.findSingle(this.el.nativeElement, '.ig-input-label');
        if (labelElement) {
            this.el.nativeElement.removeChild(labelElement);
        }

        if (this._label) {
            labelElement = document.createElement("span");
            labelElement.className = 'ig-input-label';
            labelElement.appendChild(document.createTextNode(this.label));
            this.el.nativeElement.parentNode.insertBefore(labelElement, this.el.nativeElement);
            DomHandler.removeClass(this.el.nativeElement, "ig-input-icon-only");
        } else {
            DomHandler.addClass(this.el.nativeElement, "ig-input-icon-only");
            throw new Error("Infogix Button Component: caption has not been set");
        }
    }

    @Input() get istextarea(): boolean {
        return this._istextarea;
    }

    set istextarea(val: boolean) {
        this._istextarea = val;
        if (this._istextarea) {
            DomHandler.addMultipleClasses(this.el.nativeElement, "text-area");
        }
    }

    ngOnDestroy() {
        while (this.el.nativeElement.hasChildNodes()) {
            this.el.nativeElement.removeChild(this.el.nativeElement.lastChild);
        }
    }
}

@NgModule({
    imports: [CommonModule],
    exports: [InputDirective],
    declarations: [InputDirective]
})
export class InputModule { }