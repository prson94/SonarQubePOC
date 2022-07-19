import { Directive, ElementRef, OnDestroy, OnInit } from "@angular/core";

@Directive({
	selector: "[clearTooltip]"
})
export class ClearTooltipDirective implements OnInit, OnDestroy {
	observer: MutationObserver;
	clear = null;
	tooltip = null;
	
	constructor(private el: ElementRef) {}

	ngOnInit(): void {
		this.observer = this.createMutationObserver((mutation) => {
			if (
				Array.from(mutation.addedNodes).some((element) => {
					return (<Element>element).classList.contains('p-dropdown-clear-icon');
				})
			) {
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
			}
		});

		this.observer.observe(this.el.nativeElement.querySelector('div.p-dropdown'), {childList: true});
	}

	createMutationObserver(callback: (mutation: MutationRecord) => void): MutationObserver {
		return new MutationObserver((mutations) => {
			mutations.forEach((mutation) => {
				callback(mutation);
			});
		});
	}

	ngOnDestroy(): void {
		this.observer.disconnect();
	}

}