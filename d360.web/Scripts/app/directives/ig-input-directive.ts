import { NgModule, Directive, ElementRef, AfterViewInit, OnDestroy, Input, forwardRef, Provider, HostListener } from '@angular/core';
import { DomHandler } from 'primeng/dom';
import { CommonModule } from '@angular/common';

@Directive({
    selector: '[igInput]'

})
export class InputDirective implements AfterViewInit, OnDestroy {

   
    public _size: string;
    public _invalid: boolean;
    public _validationMessage: string = "Please enter a valid value.";


    constructor(public el: ElementRef) { }

    private wrapper: HTMLDivElement;

    ngAfterViewInit() {
        if (!this.wrapper) {
            this.wrapper = document.createElement('div');
            this.wrapper.className = "ig-input-wrapper";
            this.el.nativeElement.parentNode.insertBefore(this.wrapper, this.el.nativeElement);
            this.wrapper.appendChild(this.el.nativeElement);
        }

        if (this.el.nativeElement.required) {
            this.el.nativeElement.placeholder = "Value Required";
            if (!this.el.nativeElement.classList.contains("ng-invalid"))
                DomHandler.addMultipleClasses(this.el.nativeElement, "ng-invalid");

        } else {
            this.el.nativeElement.placeholder = "Optional";
        }

        if (this.el.nativeElement.disabled) {
            DomHandler.addMultipleClasses(this.el.nativeElement, "disabled");
        } else {
            DomHandler.removeClass(this.el.nativeElement, "disabled");
        }

        DomHandler.addMultipleClasses(this.el.nativeElement, this.getStyleClass());
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


    @Input() get invalid(): boolean {
        return this._invalid;
    } 

    set invalid(val: boolean) {
        this._invalid = val;

        if (!this.wrapper) {
            this.wrapper = document.createElement('div');
            this.wrapper.className = "ig-input-wrapper";
            this.el.nativeElement.parentNode.insertBefore(this.wrapper, this.el.nativeElement);
            this.wrapper.appendChild(this.el.nativeElement);
        }

        let errorMessageEl = DomHandler.findSingle(this.wrapper, '.ig-text-required');
        if (errorMessageEl) {
            this.wrapper.removeChild(errorMessageEl);
        }

        if (this._invalid) {
            errorMessageEl = document.createElement("span");
            errorMessageEl.className = "ig-text-required ig-input-" + this._size;
            errorMessageEl.appendChild(document.createTextNode(this._validationMessage));
            this.wrapper.appendChild(errorMessageEl);
            DomHandler.addMultipleClasses(this.el.nativeElement, "invalid");
        } else {
            DomHandler.removeClass(this.el.nativeElement, "invalid");
        }
    }

    @Input() get validationMessage(): string {
        return this._validationMessage;
    }

    set validationMessage(val: string) {
        this._validationMessage = val;
    }


    @HostListener('window:keyup', ['$event'])
    keyEvent(event: KeyboardEvent) {
        if (this.el.nativeElement.required && (this.el.nativeElement.value == undefined || this.el.nativeElement.value == null || this.el.nativeElement.value == '')) {
            DomHandler.addMultipleClasses(this.el.nativeElement, "ng-invalid");
        } else {
            DomHandler.removeClass(this.el.nativeElement, "ng-invalid");
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