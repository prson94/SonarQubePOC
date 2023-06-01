import { AfterViewInit, Directive, ElementRef, NgModule, OnDestroy } from '@angular/core';
import { DomHandler } from 'primeng/dom';
import { CommonModule } from '@angular/common';

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

		if (htmlEl.tagName === "P-TRISTATECHECKBOX") {
			const minusEl = document.createElement("minusicon");
			minusEl.classList.add("p-element", "p-icon-wrapper", "p-minus-icon");
			minusEl.innerHTML = "<svg width='14' height='14' viewBox='0 0 14 14' fill='none' xmlns='http://www.w3.org/2000/svg' class='p-minus-icon p-icon' aria-hidden='true'><path d='M 0.901 7.013 C 0.9 6.682 1.169 6.413 1.5 6.411 L 12.498 6.389 C 12.829 6.387 13.098 6.656 13.099 6.987 C 13.1 7.318 12.831 7.587 12.5 7.589 L 1.502 7.611 C 1.171 7.613 0.902 7.344 0.901 7.013 Z' fill='currentColor'></path></svg>";
			checkbox.appendChild(minusEl);
		}
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