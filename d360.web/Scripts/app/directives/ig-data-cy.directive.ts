import { CommonModule } from '@angular/common';
import { AfterViewInit, Directive, ElementRef, Input, NgModule, Renderer2 } from '@angular/core';

enum PrimeComponent {
    Dropdown = 'P-DROPDOWN',
}

@Directive({
    selector: '[igDataCy]'
})
export class DataCyDirective implements AfterViewInit {
    @Input() igDataCy = '';
    readonly attr = 'data-cy';

    constructor(private el: ElementRef, private renderer: Renderer2) { }

    ngAfterViewInit(): void {
        const tagName: string = this.el.nativeElement.tagName;
        switch (tagName) {
            case PrimeComponent.Dropdown:
                const dropdown = this.el.nativeElement.querySelector('.p-dropdown');
                this.setDataCyAttr(dropdown, this.igDataCy);
                break;
            default:
                this.setDataCyAttr(this.el.nativeElement, this.igDataCy);
        }
    }

    private setDataCyAttr(el: HTMLElement, attrValue: string): void {
        this.renderer.setAttribute(el, this.attr, attrValue);
    }
}

@NgModule({
    imports: [CommonModule],
    exports: [DataCyDirective],
    declarations: [DataCyDirective]
})
export class DataCyModule { }