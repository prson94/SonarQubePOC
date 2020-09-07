import { NgModule, Directive, ElementRef, AfterViewInit, OnDestroy, Input, forwardRef, Provider, HostListener } from '@angular/core';
import { DomHandler } from 'primeng/dom';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor } from '@angular/forms';

@Directive({
    selector: '[igInput]'

})
export class InputDirective implements AfterViewInit, OnDestroy, ControlValueAccessor {

   
    public _size: string;

    protected value: string;
    protected disabled: boolean;
    onModelChange: Function = () => { };
    onModelTouched: Function = () => { };

    constructor(public el: ElementRef) { }

    writeValue(obj: string): void {
        this.value = obj;
    }

    registerOnChange(fn: any): void {
        this.onModelChange = fn;
    }

    registerOnTouched(fn: any): void {
        this.onModelTouched = fn;
    }

    setDisabledState?(isDisabled: boolean): void {
        this.disabled = isDisabled;
    }


    ngAfterViewInit() {

        DomHandler.addMultipleClasses(this.el.nativeElement, this.getStyleClass());
        if (!this.el.nativeElement.placeholder) {
            if (this.el.nativeElement.required) {
                this.el.nativeElement.placeholder = "Value Required";
            } else {
                this.el.nativeElement.placeholder = "Optional";
            }
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