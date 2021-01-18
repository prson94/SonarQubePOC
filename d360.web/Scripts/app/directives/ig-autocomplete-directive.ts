import { NgModule, Directive, ElementRef, AfterViewInit, OnDestroy, Input, forwardRef, Provider } from '@angular/core';
import { DomHandler } from 'primeng/dom';
import { CommonModule } from '@angular/common';
import { NG_VALIDATORS, AbstractControl } from '@angular/forms';

@Directive({
    selector: '[igAutocomplete]'

})
export class AutocompleteDirective implements AfterViewInit, OnDestroy {
    public _size: string;

    constructor(public el: ElementRef) { }

    ngAfterViewInit() {
        DomHandler.addMultipleClasses(this.el.nativeElement, this.getStyleClass());
        var htmlEl = this.el.nativeElement as HTMLElement;
        var input = htmlEl.getElementsByTagName('INPUT')[0];
        DomHandler.addClass(input, 'ig-input');

        DomHandler.removeClass(input, 'p-inputtext');
        DomHandler.removeClass(input, 'p-component');
        DomHandler.removeClass(input, 'p-state-default');
        DomHandler.removeClass(input, 'p-corner-all');
        DomHandler.removeClass(input, 'p-autocomplete-input');

        //remove z index from autocomplete so input within takes advantage
        htmlEl.tabIndex = -1;

        //set igSize
        if (this._size && this._size == "small") {
            DomHandler.addMultipleClasses(input, "ig-input-small");
        } else if (this._size && this._size == "medium") {
            DomHandler.addMultipleClasses(input, "ig-input-medium");
        } else if (this._size && this._size == "large") {
            DomHandler.addMultipleClasses(input, "ig-input-large");
        } else if (this._size && this._size == "full") {
            DomHandler.addMultipleClasses(input, "ig-input-full");
        }
    }

    getStyleClass(): string {
        return 'ig-autocomplete';
    }

    @Input() get igSize(): string {
        return this._size;
    }
    set igSize(val: string) {
        this._size = val;
    }

    ngOnDestroy() {
        while (this.el.nativeElement.hasChildNodes()) {
            this.el.nativeElement.removeChild(this.el.nativeElement.lastChild);
        }
    }
}

@NgModule({
    imports: [CommonModule],
    exports: [AutocompleteDirective],
    declarations: [AutocompleteDirective]
})
export class AutocompleteModule { }