import { Directive, ElementRef, OnDestroy, OnInit } from "@angular/core";
import { Dropdown } from "primeng/dropdown";
import { Subscription } from "rxjs";

@Directive({
	selector: "[clearTooltip]"
})
export class ClearTooltipDirective implements OnInit, OnDestroy {
	dropdownChange: Subscription;
	clear = null;
	tooltip = null;
	
	constructor(private el: ElementRef, private dropdown: Dropdown) {
	}

	ngOnInit(): void {
		this.dropdownChange = this.dropdown.onChange.subscribe(() => {
			if (this.dropdown.value != null && this.dropdown.showClear && !this.dropdown.disabled) {
				setTimeout(() => {
					this.clear?.removeChild(this.tooltip);
					this.clear = this.el.nativeElement.querySelector('.p-dropdown-clear-icon');
					this.tooltip = document.createElement('div');
					this.tooltip.classList.add('p-tooltip-dynamic');
					this.tooltip.append($localize`Clear`);
					this.clear.append(this.tooltip);
					this.tooltip.style.left = `-${(this.tooltip.offsetWidth - this.clear.offsetWidth) / 2}px`;
					this.tooltip.style.display = 'none';
					this.clear.addEventListener('mouseover', () => {
						this.tooltip.style.display = 'block';
					});
					this.clear.addEventListener('mouseout', () => {
						this.tooltip.style.display = 'none';
					});
				}, 10);
			}
		});
	}

	ngOnDestroy(): void {
		this.dropdownChange?.unsubscribe();
	}

}