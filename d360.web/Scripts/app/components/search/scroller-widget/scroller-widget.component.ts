import { ChangeDetectionStrategy, ChangeDetectorRef, Component, ElementRef, OnInit, ViewChild } from '@angular/core';


@Component({
	selector: "scroller-widget",
	templateUrl: "./scroller-widget.component.html",
	styleUrls: ["./scroller-widget.component.less"],
	changeDetection: ChangeDetectionStrategy.OnPush,
	host: { '(window:resize)': 'checkSize()' }
})
export class ScrollerWidgetComponent implements OnInit {

	showScrollButtons: boolean = false;
	disableScrollLeft: boolean = false;
	disableScrollRight: boolean = false;

	@ViewChild('fieldScroller', { static: false }) fieldScroller: ElementRef;

	constructor(
		private ref: ChangeDetectorRef,
		private elementRef: ElementRef) {
		
	}

	ngOnInit() {
		//Need to wait for ViewChildren, but can't use AfterOnInit
		setTimeout(() => {
			this.checkSize();
		});
	}

	/* Field scroller section */

	checkSize() {
		if (this.fieldScroller) {
			const maxWidth = this.getElementRightPosition(this.fieldScroller.nativeElement.parentElement);
			const lastTab = this.getElementRightPosition(this.fieldScroller.nativeElement.lastElementChild);
			this.showScrollButtons = lastTab > maxWidth;
		}
		this.checkScrollPos();
	}

	checkScrollPos() {
		if (this.fieldScroller) {
			const currentPosition = this.fieldScroller.nativeElement.scrollLeft;
			this.disableScrollLeft = currentPosition === 0;

			const maxWidth = this.getElementRightPosition(this.fieldScroller.nativeElement.parentElement);
			const lastTab = this.getElementRightPosition(this.fieldScroller.nativeElement.lastElementChild);
			this.disableScrollRight = lastTab <= maxWidth;

			this.ref.markForCheck();
		}
	}

	private getElementRightPosition(element) {
		if (element && element.getBoundingClientRect) {
			return element.getBoundingClientRect().right;
		}
		return NaN;
	}

	private getElementWidth(element) {
		if (element && element.getBoundingClientRect) {
			return element.getBoundingClientRect().right - element.getBoundingClientRect().left;
		}
		return NaN;
	}

	scroll(direction: string) {
		const el = this.fieldScroller.nativeElement;
		let scrollAmount = 0;
		const scrollDistance = Math.floor(this.getElementWidth(el) * 0.95);
		const move = () => {
			if (direction === 'L') {
				el.scrollLeft -= 10;
			} else {
				el.scrollLeft += 10;
			}
			scrollAmount += 10;
			if (scrollAmount >= scrollDistance) {
				this.checkScrollPos();
				window.clearInterval(id);
			}
			this.checkScrollPos();
		};

		const id = window.setInterval(move, 5);
	}
}
