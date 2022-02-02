import { CommonModule } from '@angular/common';
import { AfterViewInit, ChangeDetectorRef, Directive, ElementRef, NgModule, Renderer2 } from '@angular/core';

@Directive({
    selector: '[igDataCy]'
})
export class DataCyDirective implements AfterViewInit{
    constructor(private el: ElementRef, private renderer: Renderer2) {
        
    }

    ngAfterViewInit(): void {
        const tagName = this.el.nativeElement.tagName;
        switch (tagName) {
            case 'P-DROPDOWN':
                const dropdown = this.el.nativeElement.querySelector('.p-dropdown');
                setTimeout(() => {
                    this.renderer.setAttribute(dropdown, 'data-cy', 'wip');
                }, 0)
                
                break;
            case 'Papayas':
                console.log('Mangoes and papayas are $2.79 a pound.');
                break;
            default:
                this.el.nativeElement.style.backgroundColor = 'yellow';
        }
        // this.el.nativeElement.style.backgroundColor = 'red';
        console.log(this.el);
        console.log(this.el.nativeElement.tagName);
        // P-DROPDOWN
    }
}

@NgModule({
    imports: [CommonModule],
    exports: [DataCyDirective],
    declarations: [DataCyDirective]
})
export class DataCyModule { }