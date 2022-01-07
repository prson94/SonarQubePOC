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
    hoverTooltipWidth: number = 350;

    private isTagTooltip: boolean = false;

    contextMenuItems: any[] = [
        { title: 'View Information', value: 'info' },
        { title: 'Open', value: 'open' },
        { title: 'Open in New Tab', value: 'new-tab' }
    ]

    constructor(private el: ElementRef,
        @Inject(DOCUMENT) private document: Document,
        private renderer: Renderer2) { }

    ngOnInit() {
        var htmlEl = this.el.nativeElement as HTMLElement;
        htmlEl.onmouseenter = () => {
            this.addTooltip();
        };

        htmlEl.onmouseleave = () => {
            this.removeTooltip();
        };
        DomHandler.addClass(htmlEl, 'has-context');
    }

    addTooltip() {
        this.hoverElement = this.document.createElement('div');
        this.hoverElement.style.display = "block";
        this.hoverElement.style.position = "fixed";
        this.hoverElement.style.width = this.hoverTooltipWidth + "px";

        this.renderer.setAttribute(this.hoverElement, 'class', 'link-context-menu-p-tooltip p-tooltip p-component p-tooltip-top ig-tooltip');

        var hoverItem = this.document.createElement('div');
        this.renderer.setAttribute(hoverItem, 'class', 'p-tooltip-text');

        var value = this.el.nativeElement.dataset['tooltip'];
        var html = "";
        if (value) {
            html += value + "</br>";
            this.isTagTooltip = true;
        }
        html += "Click the link to view information in the side panel or right-click for more options";

        hoverItem.innerHTML = html;
        this.hoverElement.appendChild(hoverItem);
        this.renderer.appendChild(this.document.body, this.hoverElement);
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
            if (item.value === "info") {
                menuItem.style.fontWeight = "700";
            }
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

            if (this.isTagTooltip) {
                this.hoverElement.style.top = (box.top - 84) + "px";
            }

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