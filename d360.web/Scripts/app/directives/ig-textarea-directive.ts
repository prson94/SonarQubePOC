import { NgModule, Directive, ElementRef, AfterViewInit, Input, OnDestroy, Renderer2 } from '@angular/core';
import { DomHandler } from 'primeng/dom';
import { CommonModule } from '@angular/common';

@Directive({
    selector: '[igTextArea]'
})
export class TextAreaDirective implements AfterViewInit, OnDestroy {

    @Input() required: boolean;

    @Input() disabled: boolean;

	@Input() autoResize: boolean;
	
	resizeObserver: ResizeObserver;
	
	collapsed: boolean = false;
	
	baseAutoResizeHeight: number = 160;

    constructor(private el: ElementRef, private renderer: Renderer2) { }

    ngAfterViewInit() {
        DomHandler.addMultipleClasses(this.el.nativeElement, this.getStyleClass());

        this.required = this.el.nativeElement.getAttribute("required");
        this.disabled = this.el.nativeElement.getAttribute("disabled");

        if (this.required == null) {
            this.el.nativeElement.setAttribute("placeholder", $localize`Optional`);
        } else {
            this.el.nativeElement.setAttribute("placeholder", $localize`Value required`);
            this.el.nativeElement.setAttribute("aria-required", true);
        }

		if (this.autoResize) {
			this.applyAutoResizeBehavior();
        }
    }
	
	applyAutoResizeBehavior(): void {
		this.renderer.addClass(this.el.nativeElement, 'autoresize');

		const parent = this.el.nativeElement.parentNode;
		const divElement = this.renderer.createElement("div");
		this.renderer.addClass(divElement, `${this.getStyleClass()}-wrapper`);
		this.renderer.insertBefore(parent, divElement, this.el.nativeElement);
		this.renderer.removeChild(parent, this.el.nativeElement);
		this.renderer.appendChild(divElement, this.el.nativeElement);

		const collapseButton = this.createCollapseButton();
		
		collapseButton.addEventListener('click', () => {
			this.collapsed = !this.collapsed;
			if (this.collapsed) {
				this.renderer.addClass(this.el.nativeElement, 'locked');
			} else {
				this.renderer.removeClass(this.el.nativeElement, 'locked');
			}
			collapseButton.textContent = this.getCollapseButtonLabel();
			collapseButton.appendChild(this.getCollapseButtonIcon());
		});
		
		this.renderer.appendChild(divElement, collapseButton);

		this.resizeObserver = new ResizeObserver(($event) => {
			this.el.nativeElement.style.height = 'auto';
			this.el.nativeElement.style.height = this.el.nativeElement.scrollHeight + 'px';
			const isCollapseButtonVisible = $event[0].target.scrollHeight > this.baseAutoResizeHeight;
			if (isCollapseButtonVisible) {
				this.renderer.addClass(collapseButton, 'visible');
			} else {
				this.renderer.removeClass(collapseButton, 'visible');
			}
		});
		
		this.resizeObserver.observe(this.el.nativeElement);
	}
	
	createCollapseButton(): HTMLElement {
		const collapseButton = document.createElement('div');
		collapseButton.textContent = this.getCollapseButtonLabel();
		collapseButton.appendChild(this.getCollapseButtonIcon());
		collapseButton.classList.add(`${this.getStyleClass()}-collapse`);
		return collapseButton;
	}
	
	getCollapseButtonIcon(): HTMLElement {
		const icon = document.createElement('i');
		icon.classList.add(`${this.getStyleClass()}-collapse-icon`);
		icon.classList.add('fa');
		if (this.collapsed) {
			icon.classList.add('fa-chevron-down');
		} else {
			icon.classList.add('fa-chevron-up');
		}
		return icon;
	}
	
	getCollapseButtonLabel(): string {
		let label = $localize`text box`;
		if (this.collapsed) {
			label = $localize`expand ` + label;
		} else {
			label = $localize`collapse ` + label;
		}
		return label;
	}

    getStyleClass(): string {
        return 'ig-textarea';
    }

	ngOnDestroy(): void {
		this.resizeObserver?.disconnect();
	}
}

@NgModule({
    imports: [CommonModule],
    exports: [TextAreaDirective],
    declarations: [TextAreaDirective]
})
export class TextAreaModule { }