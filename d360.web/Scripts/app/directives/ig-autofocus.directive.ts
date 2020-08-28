import { Directive, ElementRef, DoCheck, ChangeDetectorRef } from '@angular/core';

@Directive({
    selector: '[igAutoFocus]'

})
export class AutoFocusDirective implements DoCheck {

    private isVisible: boolean = false;

    constructor(private el: ElementRef, private cdRef: ChangeDetectorRef) {
    }

    ngDoCheck() {
        var currentState = this.isVisible;
        this.isVisible = !this.isElementHidden(this.el.nativeElement as HTMLElement);
        if (currentState != this.isVisible && this.isVisible === true) {
            this.focusElement();
        }
    }

    private isElementHidden(element: HTMLElement): boolean {
        if (element) {
            if (window.getComputedStyle(element)['visibility'] == 'hidden') {
                return true
            }
            return this.isElementHidden(element.parentElement);
        }
        return false;
    }

    private focusElement() {
        var htmlElement = (this.el.nativeElement as HTMLElement);
        var tagName = htmlElement.tagName;
        if (tagName === 'P-AUTOCOMPLETE') {
            var inputF = htmlElement.getElementsByTagName('input');
            if (inputF && inputF.length != 0) {
                inputF[0].focus();
            }
        }
        else {
            this.el.nativeElement.focus();
        }
    }


}