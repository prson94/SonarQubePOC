import { NgModule, Directive, ElementRef, AfterViewInit, Input, ChangeDetectorRef, OnChanges } from '@angular/core';
import { DomHandler } from 'primeng/dom';
import { CommonModule } from '@angular/common';
import { Dropdown } from 'primeng/dropdown';

@Directive({
    selector: '[igDropdown]'
})
export class DropdownDirective implements AfterViewInit {


    public _size: string;
    @Input() required: boolean;
    @Input() disabled: boolean;
    private isOverlayVisible: boolean = false;

    constructor(public el: ElementRef, public dropdownRef: Dropdown, private ref: ChangeDetectorRef) { }

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

        setInterval(() => {
            if (this.isOverlayVisible !== this.dropdownRef.overlayVisible) {
                if (this.dropdownRef.overlayVisible && this.dropdownRef.overlay.className.indexOf('ig-dropdown-overlay') == -1) {
                    this.dropdownRef.overlay.classList.add('ig-dropdown-overlay');
                }
                this.isOverlayVisible = this.dropdownRef.overlayVisible;
            }

        }, 10);

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