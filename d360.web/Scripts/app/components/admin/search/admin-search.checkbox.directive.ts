import { AfterContentChecked, Directive, Input } from "@angular/core";
import { TTCheckbox } from "primeng/treetable";

@Directive({
	selector: '[adminSearchCheckbox]'
})

export class AdminSearchCheckboxDirective implements AfterContentChecked {
	@Input() canRebuild: boolean;
	constructor(private checkbox: TTCheckbox) {}
	
	// PrimeNG allows disabled rows to be selected, which is improper behaviour,
	// that is why we need to handle check state of checkboxes additionally
	ngAfterContentChecked(): void {
		if (!this.canRebuild) {
			this.checkbox.checked = false;
		}
	}
}
