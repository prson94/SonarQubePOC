import { AfterContentChecked, Directive } from "@angular/core";
import { TTCheckbox } from "primeng/treetable";

@Directive({
	selector: '[adminSearchCheckbox]'
})

export class AdminSearchCheckboxDirective implements AfterContentChecked {

	constructor(private checkbox: TTCheckbox) {}

	// PrimeNG allows disabled rows to be selected, which is improper behaviour,
	// that is why we need to handle check state of checkboxes additionally
	ngAfterContentChecked(): void {
		if (this.checkbox.checked && this.checkbox.disabled) {
			this.checkbox.checked = false;
		}
	}
}
