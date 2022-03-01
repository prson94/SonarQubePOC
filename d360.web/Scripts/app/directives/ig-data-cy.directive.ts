import { CommonModule } from '@angular/common';
import { AfterViewInit, Directive, ElementRef, Input, NgModule, OnDestroy, Renderer2 } from '@angular/core';

enum PrimeComponent {
    Dropdown = 'P-DROPDOWN',
    Editor = 'P-EDITOR',
    Table = 'P-TABLE',
    Checkbox = 'P-CHECKBOX',
}

@Directive({
    selector: '[igDataCy]'
})
export class DataCyDirective implements AfterViewInit, OnDestroy {
    @Input() igDataCy = '';
    readonly attr = 'data-cy';
    paginatorMutationObserver: MutationObserver;

    constructor(private el: ElementRef, private renderer: Renderer2) { }

    ngAfterViewInit(): void {
        const nativeElement = this.el.nativeElement;
        const tagName: string = nativeElement.tagName;
        switch (tagName) {
            case PrimeComponent.Dropdown:
                const dropdown = nativeElement.querySelector('.p-dropdown');
                this.setDataCyAttr(dropdown, this.igDataCy);
                break;
            case PrimeComponent.Editor:
                const editorContent = nativeElement.querySelector('.p-editor-content');
                this.setDataCyAttr(editorContent, this.igDataCy);
                break;
            case PrimeComponent.Table:
                this.setDataCyAttrToPaginator(nativeElement);
                break;
            case PrimeComponent.Checkbox:
                const checkboxLabel = nativeElement.querySelector('.p-checkbox-label');
                this.setDataCyAttr(checkboxLabel, this.igDataCy);
                break;
            default:
                this.setDataCyAttr(nativeElement, this.igDataCy);
        }
    }

    private setDataCyAttr(el: HTMLElement, attrValue: string): void {
        this.renderer.setAttribute(el, this.attr, attrValue);
    }

    private setDataCyAttrToPaginator(nativeElement: HTMLElement): void {
        const paginator = nativeElement.querySelector('.p-paginator');
        if (paginator) {
            this.setDataCyAttrToPaginatorPages(paginator);
            this.setDataCyAttrToPaginatorDropdown(paginator);
            this.setDataCyAttrToMutatedPaginatorPages(paginator)
        }
    }

    private setDataCyAttrToPaginatorPages(paginator: Element): void {
        const paginatorPages: NodeListOf<HTMLElement> = paginator.querySelectorAll('.p-paginator-page');
        paginatorPages.forEach((paginatorPage) => {
            this.setDataCyAttr(paginatorPage, this.igDataCy + paginatorPage.textContent);
        });
    }

    private setDataCyAttrToMutatedPaginatorPages(paginator: Element): void {
        this.paginatorMutationObserver = new MutationObserver(mutations => {
            mutations.forEach((mutation) => {
                const addedNode = mutation.addedNodes[0] as HTMLElement;
                if (addedNode && addedNode.setAttribute !== undefined) {
                    this.setDataCyAttr(addedNode, this.igDataCy + addedNode.textContent);
                }
            });
        });
        var config = { childList: true, subtree : true };
        this.paginatorMutationObserver.observe(paginator, config);
    }

    private setDataCyAttrToPaginatorDropdown(paginator: Element): void {
        const paginatorDropdown = paginator.querySelector('.p-dropdown') as HTMLElement;
        this.setDataCyAttr(paginatorDropdown, this.igDataCy + 'Dropdown');
    }

    ngOnDestroy() {
        this.paginatorMutationObserver?.disconnect();
    }
}

@NgModule({
    imports: [CommonModule],
    exports: [DataCyDirective],
    declarations: [DataCyDirective]
})
export class DataCyModule { }
