import { AfterViewInit, Directive, ElementRef } from "@angular/core";
import { DomHandler } from "primeng/dom";

@Directive({
	selector: '[adminSearchTreeTable]'
})

export class AdminSearchTreeTableDirective implements AfterViewInit {
	constructor(private element: ElementRef) {}
	
	ngAfterViewInit(): void {
		const scrollableBody = DomHandler.findSingle(this.element.nativeElement, '.p-treetable-scrollable-body');
		scrollableBody.style.overflowX = 'hidden';
		scrollableBody.style.overflowY = 'auto';
	}
	
}
