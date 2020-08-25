import { NgModule, Directive, ElementRef, AfterViewInit, OnDestroy, Input, forwardRef, Provider } from '@angular/core';
import { DomHandler } from 'primeng/dom';
import { CommonModule } from '@angular/common';
import { NG_VALIDATORS, AbstractControl } from '@angular/forms';

@Directive({
    selector: '[igAutocomplete]'

})
export class AutocompleteDirective implements AfterViewInit, OnDestroy {

    constructor(public el: ElementRef) { }

    ngAfterViewInit() {
        DomHandler.addMultipleClasses(this.el.nativeElement, this.getStyleClass());
        var htmlEl = this.el.nativeElement as HTMLElement;
        var input = htmlEl.getElementsByTagName('INPUT')[0];
        DomHandler.addClass(input, 'ig-input');

        DomHandler.removeClass(input, 'ui-inputtext');
        DomHandler.removeClass(input, 'ui-widget');
        DomHandler.removeClass(input, 'ui-state-default');
        DomHandler.removeClass(input, 'ui-corner-all');
        DomHandler.removeClass(input, 'ui-autocomplete-input');

        //remove z index from autocomplete so input within takes advantage
        htmlEl.tabIndex = -1;
    }

    getStyleClass(): string {
        return 'ig-autocomplete';
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