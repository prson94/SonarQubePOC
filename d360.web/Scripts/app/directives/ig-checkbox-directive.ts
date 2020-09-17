import { NgModule, Directive, ElementRef, AfterViewInit, OnDestroy, Input, forwardRef, Provider, HostListener } from '@angular/core';
import { DomHandler } from 'primeng/dom';
import { CommonModule } from '@angular/common';
import { NG_VALIDATORS, AbstractControl } from '@angular/forms';

@Directive({
    selector: '[igCheckbox]'

})
export class CheckboxDirective implements AfterViewInit, OnDestroy {

    constructor(public el: ElementRef) { }

    ngAfterViewInit() {
        var htmlEl = this.el.nativeElement as HTMLElement;
        var checkboxcontainer = htmlEl.firstChild;
        var checkbox = checkboxcontainer.lastChild;
        DomHandler.addClass(checkbox, 'ig-checkbox');
        //remove z index from checkbox so input within takes advantage
        htmlEl.tabIndex = -1;
    }

    getStyleClass(): string {
        return 'ig-checkbox';
    }

    ngOnDestroy() {
        while (this.el.nativeElement.hasChildNodes()) {
            this.el.nativeElement.removeChild(this.el.nativeElement.lastChild);
        } 
    }
}

@NgModule({
    imports: [CommonModule],
    exports: [CheckboxDirective],
    declarations: [CheckboxDirective]
})
export class IgCheckboxModule { }