import { AfterViewInit, Directive, ElementRef } from "@angular/core";
import { DomHandler } from "primeng/dom";

@Directive({
	selector: '[adminSearchTreeTable]'
})

export class AdminSearchTreeTableDirective implements AfterViewInit {
	constructor(private element: ElementRef) {}
	
	ngAfterViewInit(): void {
		const scrollableBody = DomHandler.findSingle(this.element.nativeElement, '.p-treetable-scrollable-body');
		const scrollableHead = DomHandler.findSingle(this.element.nativeElement, '.p-treetable-scrollable-header-box');
		scrollableBody.style.overflowX = 'hidden';
		scrollableBody.style.overflowY = 'overlay';
		scrollableBody.style.paddingRight = '17px';
		scrollableHead.style.paddingRight = '17px';
	}
	
}
