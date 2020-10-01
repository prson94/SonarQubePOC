import { NgModule, Directive, ElementRef, AfterViewInit, OnDestroy, Input, forwardRef, Provider, HostListener } from '@angular/core';
import { DomHandler } from 'primeng/dom';
import { CommonModule } from '@angular/common';
import { NG_VALIDATORS, AbstractControl } from '@angular/forms';

@Directive({
    selector: '[igRadioButton]'

})
export class RadioButtonDirective implements AfterViewInit, OnDestroy {

    constructor(public el: ElementRef) { }

    ngAfterViewInit() {
        var htmlEl = this.el.nativeElement as HTMLElement;
        var radiobtncontainer = htmlEl.firstChild;
        var radioBtn = radiobtncontainer.lastChild;
        DomHandler.addClass(radioBtn, 'ig-radio-button');
        //remove z index from checkbox so input within takes advantage
        htmlEl.tabIndex = -1;
    }

    getStyleClass(): string {
        return 'ig-radio-button';
    }

    ngOnDestroy() {
        while (this.el.nativeElement.hasChildNodes()) {
            this.el.nativeElement.removeChild(this.el.nativeElement.lastChild);
        } 
    }
}

@NgModule({
    imports: [CommonModule],
    exports: [RadioButtonDirective],
    declarations: [RadioButtonDirective]
})
export class IgRadioButtonModule { }