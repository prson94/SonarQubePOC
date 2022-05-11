import { NgModule, Directive, ElementRef, AfterViewInit, OnDestroy, Input, ChangeDetectorRef, HostListener } from '@angular/core';
import { DomHandler } from 'primeng/dom';
import { CommonModule } from '@angular/common';

@Directive({
    selector: '[igInput]'
})
export class InputDirective implements AfterViewInit, OnDestroy {


    public _size: string;
    @Input() required: boolean;
    @Input() disabled: boolean;

    constructor(public el: ElementRef, private ref: ChangeDetectorRef) { }

    setDisabledState?(isDisabled: boolean): void {
        this.disabled = isDisabled;
    }


    ngAfterViewInit() {
        DomHandler.addMultipleClasses(this.el.nativeElement, this.getStyleClass());
        this.required = this.el.nativeElement.getAttribute("required");
        this.disabled = this.el.nativeElement.getAttribute("disabled");
        var placeholder = this.el.nativeElement.getAttribute("placeholder");

        if (!placeholder && placeholder != '') {
            if (this.required == null) {
                this.el.nativeElement.setAttribute("placeholder", $localize`Optional`);
            } else {
                this.el.nativeElement.setAttribute("placeholder", $localize`Value required`);
            }
        }

        if (this.required) {
            this.el.nativeElement.setAttribute("aria-required", true);
        }

    }

    getStyleClass(): string {
        return 'ig-input';
    }

    @Input() get igSize(): string {
        return this._size;
    }
    set igSize(val: string) {
        this._size = val;
        if (this._size && this._size == "small") {
            DomHandler.addMultipleClasses(this.el.nativeElement, "ig-input-small");
        } else if (this._size && this._size == "medium") {
            DomHandler.addMultipleClasses(this.el.nativeElement, "ig-input-medium");
        } else if (this._size && this._size == "large") {
            DomHandler.addMultipleClasses(this.el.nativeElement, "ig-input-large");
        } else if (this._size && this._size == "full") {
            DomHandler.addMultipleClasses(this.el.nativeElement, "ig-input-full");
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