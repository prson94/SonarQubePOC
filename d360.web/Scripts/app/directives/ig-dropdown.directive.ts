import { NgModule, Directive, ElementRef, AfterViewInit, Input, ChangeDetectorRef } from '@angular/core';
import { DomHandler } from 'primeng/dom';
import { CommonModule } from '@angular/common';

@Directive({
    selector: '[igDropdown]'
})
export class DropdownDirective implements AfterViewInit {

   
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

        if (this.required == null) {
            this.el.nativeElement.setAttribute("placeholder", "Optional");
        } else {
            this.el.nativeElement.setAttribute("placeholder", "Value required");
            this.el.nativeElement.setAttribute("aria-required", true);
        }

    }

    getStyleClass(): string {
        return 'ig-dropdown';
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
}

@NgModule({
    imports: [CommonModule],
    exports: [DropdownDirective],
    declarations: [DropdownDirective]
})
export class DropdownModule { }