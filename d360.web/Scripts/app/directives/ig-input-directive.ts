import { NgModule, Directive, ElementRef, AfterViewInit, OnDestroy, Input, forwardRef, Provider } from '@angular/core';
import { DomHandler } from 'primeng/dom';
import { CommonModule } from '@angular/common';
import { NG_VALIDATORS, AbstractControl } from '@angular/forms';

@Directive({
    selector: '[igInput]'

})
export class InputDirective implements AfterViewInit, OnDestroy {

    @Input() tooltip: string;

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