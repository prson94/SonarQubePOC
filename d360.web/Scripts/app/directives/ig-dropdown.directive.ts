import { NgModule, Directive, ElementRef, AfterViewInit, Input, ChangeDetectorRef, AfterContentInit } from "@angular/core";
import { DomHandler } from "primeng/dom";
import { CommonModule } from "@angular/common";
import { Dropdown } from "primeng/dropdown";
import { Page } from "powerbi-client";

@Directive({
    selector: "[igDropdown]"
})
export class DropdownDirective implements AfterContentInit {


    public _size: string;
    @Input() required: boolean;
    @Input() disabled: boolean;
    @Input() overlayLowerZIndex: boolean = false;
    @Input() ellipsisDirection: string = "ltr";
    constructor(public el: ElementRef, public dropdownRef: Dropdown, private ref: ChangeDetectorRef) { }

    setDisabledState?(isDisabled: boolean): void {
        this.disabled = isDisabled;
    }

    ngAfterContentInit(): void {
        DomHandler.addMultipleClasses(this.el.nativeElement, this.getStyleClass());
        if (this.required !== null && (typeof this.required !== undefined) && this.required?.toString() !== "") {
            //If required was set by Angular binding via [required] input parameter
            if (this.required) {
                (this.el.nativeElement as HTMLElement).setAttribute("required", "true");
            }
            else {
                (this.el.nativeElement as HTMLElement).removeAttribute("required");
            }
        }

        this.required = this.el.nativeElement.getAttribute("required");
        this.disabled = this.el.nativeElement.getAttribute("disabled");
        var tabIndex = this.el.nativeElement.getAttribute("tabIndex");

        var placeholder = this.el.nativeElement.getAttribute("placeholder");
        this.el.nativeElement.tabIndex = -1;
        this.dropdownRef.tabindex = tabIndex;
        let isPlaceholderSet = !(placeholder == undefined || placeholder == null || placeholder == "");

        if (!isPlaceholderSet) {
            if (this.required == null) {
                this.dropdownRef.placeholder = $localize`Optional`;
                this.dropdownRef.showClear = true;
            } else {
                this.dropdownRef.placeholder = $localize`Value required`;
                this.dropdownRef.showClear = false;
                this.el.nativeElement.setAttribute("aria-required", true);
            }
        }
        this.dropdownRef.scrollHeight = "340px";

        setInterval(() => {
            if (this.dropdownRef.overlayVisible && this.dropdownRef?.overlay) {
                if (this.dropdownRef.overlay.className.indexOf("ig-dropdown-overlay") == -1) {
                    this.dropdownRef.overlay.classList.add("ig-dropdown-overlay");

                    if (this.ellipsisDirection === "ltr") {
                        this.dropdownRef.overlay.classList.add("ig-dropdown-ellipsis-ltr");
                    }
                    else {
                        this.dropdownRef.overlay.classList.add("ig-dropdown-ellipsis-rtl");
                    }

                    if (this.overlayLowerZIndex) {
                        this.dropdownRef.overlay.classList.add("ig-dropdown-overlay-lower-index");
                    }
                    var input = this.dropdownRef.overlay.getElementsByTagName("input")[0];

                    if (input)
                        input.className = "ig-input";
                }

            }

            let count: number = this.getItemsCount();
            if (count > 10) {
                this.dropdownRef.filter = true;
                this.dropdownRef.filterPlaceholder = $localize`Search fields`;
            }
            else {
                this.dropdownRef.filter = false;
            }
        }, 10);

    }

    getItemsCount() {
        if (!this.dropdownRef?.options?.length) {
            return 0;
        }
        let count: number = 0;
        this.dropdownRef.options.forEach((opt) => {
            if (!opt.items) {
                count++;
            }
            else {
                count += +opt.items.length;
            }
        });
        return count;
    }

    getStyleClass(): string {
        return "ig-dropdown";
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