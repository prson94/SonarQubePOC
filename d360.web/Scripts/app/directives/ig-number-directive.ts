import { NgModule, Directive, AfterViewInit, OnDestroy, ElementRef, ChangeDetectorRef, forwardRef, HostListener} from '@angular/core';
import { CommonModule } from '@angular/common';
import { DomHandler } from 'primeng/dom';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';


@Directive({
    selector: '[igNumberField]'

})
export class NumberFieldDirective implements AfterViewInit, OnDestroy, ControlValueAccessor{

    protected value: number;
    protected disabled: boolean;
    onModelChange: Function = () => { };
    onModelTouched: Function = () => { };

    constructor(public el: ElementRef, private ref: ChangeDetectorRef) { }

    writeValue(obj: number): void {
        this.value = obj;
        this.el.nativeElement.dispatchEvent(new Event('input'));
        this.ref.markForCheck();    
    }

    registerOnChange(fn: any): void {
        this.onModelChange = fn;
    }

    registerOnTouched(fn: any): void {
        this.onModelTouched = fn;
    }

    @HostListener('input', ['$event'])
    onInputChange() {
        this.value = this.el.nativeElement.value;
        this.ref.markForCheck();
    }

    setDisabledState?(isDisabled: boolean): void {
        this.disabled = isDisabled;
    }

    ngAfterViewInit() {

        let container = <HTMLElement>this.el.nativeElement.nextElementSibling;
        if (container && container.classList.contains("ig-button-container")) {
            this.removeElementChildren(container);
            container.parentNode.removeChild(container);
        }

        this.el.nativeElement.type = "number";

        container = document.createElement("div");
        container.className = 'ig-button-container';
        let buttonUp = document.createElement("div");
        let faUp = document.createElement("span");
        buttonUp.addEventListener("click", (e) => {
            this.increment();
        });
        buttonUp.className = "ig-number-field-button up";
        faUp.className = "fa fa-chevron-up";
        buttonUp.appendChild(faUp);
        let buttonDown = document.createElement("div");
        let faDown = document.createElement("span");
        buttonDown.addEventListener("click", (e) => {
            this.decrement();
        });
        buttonDown.className = "ig-number-field-button down";
        faDown.className = "fa fa-chevron-down";
        buttonDown.appendChild(faDown)

        container.appendChild(buttonUp);
        container.appendChild(buttonDown);
        this.el.nativeElement.parentNode.insertBefore(container, this.el.nativeElement.nextSibling)

       

        DomHandler.addMultipleClasses(this.el.nativeElement, this.getStyleClass());
        if (!this.el.nativeElement.placeholder) {
            if (this.el.nativeElement.required) {
                this.el.nativeElement.placeholder = "Value Required";
            } else {
                this.el.nativeElement.placeholder = "Optional";
            }
        }
        this.ref.markForCheck();
    }

    increment() {
        this.el.nativeElement.stepUp();
        this.writeValue(this.el.nativeElement.value);
    }



    decrement() {
        this.el.nativeElement.stepDown();
        this.writeValue(this.el.nativeElement.value);
    }

    getStyleClass(): string {
        return 'ig-number-field';
    }

    ngOnDestroy() {
        this.removeElementChildren(this.el.nativeElement);
    }

    removeElementChildren(elem: any) {
        while (elem.hasChildNodes()) {
            elem.removeChild(this.el.nativeElement.lastChild);
        }
    }
}

@NgModule({
    imports: [CommonModule],
    exports: [NumberFieldDirective],
    declarations: [NumberFieldDirective]
})
export class InputModule { }