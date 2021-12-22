import { DOCUMENT } from '@angular/common';
import { AfterViewChecked, OnDestroy } from '@angular/core';
import { Inject, OnInit, Renderer2 } from '@angular/core';
import { Directive, ElementRef, HostListener } from '@angular/core';
import { DomHandler } from 'primeng/dom';

@Directive({
    selector: '[context-link]'
})
export class LinkWithContextDirective implements OnInit, OnDestroy, AfterViewChecked {
    contextElement: HTMLDivElement;
    hoverElement: HTMLDivElement;
    hoverTooltipWidth: number = 300;

    contextMenuItems: any[] = [
        { title: 'View Information', value: 'info' },
        { title: 'Open', value: 'open' },
        { title: 'Open in New Tab', value: 'new-tab' }
    ]

    constructor(private el: ElementRef,
        @Inject(DOCUMENT) private document: Document,
        private renderer: Renderer2) { }

    //<div class="p-tooltip p-component p-tooltip-top ig-tooltip" style = "display: inline-block; left: 484.828px; top: 444px; opacity: 1.048; z-index: 1012;" >
    //<div class="p-tooltip-arrow" > </div><div class="p-tooltip-text">Export to Excel</div > </div>

    ngOnInit() {
        var htmlEl = this.el.nativeElement as HTMLElement;
        htmlEl.onmouseenter = () => {
            this.hoverElement = this.document.createElement('div');
            this.hoverElement.style.display = "block";
            this.hoverElement.style.position = "fixed";
            this.hoverElement.style.width = this.hoverTooltipWidth + "px";

            this.renderer.setAttribute(this.hoverElement, 'class', 'link-context-menu-p-tooltip p-tooltip p-component p-tooltip-top ig-tooltip');

            var hoverItem = this.document.createElement('div');
            this.renderer.setAttribute(hoverItem, 'class', 'p-tooltip-text');
            hoverItem.innerHTML = "Click the link to view information in the side panel or right-click for more options";
            this.hoverElement.appendChild(hoverItem);
            this.renderer.appendChild(this.document.body, this.hoverElement);
        };

        htmlEl.onmouseleave = () => {
            this.removeTooltip();
        };
        DomHandler.addClass(htmlEl, 'has-context');
    }

    @HostListener('contextmenu', ['$event.target'])
    onContextClick($event) {
        this.removeElement();
        var htmlEl = (this.el.nativeElement as HTMLElement);
        if (htmlEl.classList.contains('visible')) {
            htmlEl.classList.remove('visible');
        }
        else {
            htmlEl.classList.add('visible');
        }



        this.contextElement = this.document.createElement('div');
        this.renderer.setAttribute(this.contextElement, 'class', 'link-context-menu');

        this.contextMenuItems.forEach((item) => {
            var menuItem = this.document.createElement('div');
            this.renderer.setAttribute(menuItem, 'class', 'menu-item');
            menuItem.innerHTML = item.title;
            menuItem.onclick = ($event) => { this.menuItemClicked($event, item.value); };
            this.contextElement.appendChild(menuItem);
        });


        this.renderer.appendChild(this.document.body, this.contextElement);

        this.updatePosition();

        return false;
    }

    menuItemClicked($event, type) {
        let event = new MouseEvent('click', { bubbles: true });
        event['from-context-method'] = type;
        this.el.nativeElement.dispatchEvent(event);
    }

    @HostListener('document:click', ['$event.target'])
    onClick(btn) {
        this.removeElement();
    }

    ngOnDestroy() {
        this.removeElement();
        this.removeTooltip();
    }

    ngAfterViewChecked() {
        this.updatePosition();
    }

    updatePosition() {
        var htmlEl = (this.el.nativeElement as HTMLElement);
        if (htmlEl && this.contextElement) {
            var box = htmlEl.getBoundingClientRect();
            this.contextElement.style.top = (box.top + box.height) + "px";
            this.contextElement.style.left = box.left + "px";
        }

        if (htmlEl && this.hoverElement) {
            var box = htmlEl.getBoundingClientRect();
            this.hoverElement.style.top = (box.top - 62) + "px";
            this.hoverElement.style.left = (box.left + (box.width / 2) - (this.hoverTooltipWidth / 2)) + "px";
        }
    }

    removeElement() {
        const elements = document.getElementsByClassName('link-context-menu');
        while (elements.length > 0) {
            elements[0].parentNode.removeChild(elements[0]);
        }
    }

    removeTooltip() {
        const elements = document.getElementsByClassName('link-context-menu-p-tooltip');
        while (elements.length > 0) {
            elements[0].parentNode.removeChild(elements[0]);
        }
    }
}