import { NgModule, Directive, AfterViewInit, OnDestroy, ElementRef} from '@angular/core';
import { CommonModule } from '@angular/common';
import { DomHandler } from 'primeng/dom';
import { ControlValueAccessor } from '@angular/forms';

@Directive({
    selector: '[igNumberField]'

})
export class NumberFieldDirective implements AfterViewInit, OnDestroy, ControlValueAccessor {

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
        return 'ig-number-field';
    }

    ngOnDestroy() {
        while (this.el.nativeElement.hasChildNodes()) {
            this.el.nativeElement.removeChild(this.el.nativeElement.lastChild);
        }
    }
}

@NgModule({
    imports: [CommonModule],
    exports: [NumberFieldDirective],
    declarations: [NumberFieldDirective]
})
export class InputModule { }