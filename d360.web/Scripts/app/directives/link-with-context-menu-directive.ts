import { DOCUMENT } from '@angular/common';
import { AfterViewChecked, OnDestroy } from '@angular/core';
import { Inject, OnInit, Renderer2 } from '@angular/core';
import { Directive, ElementRef, HostListener } from '@angular/core';
import { DomHandler } from 'primeng/dom';
import { MenuItemContent } from 'primeng/menu';
import { HTML } from '../models/fieldtype-api.model';

@Directive({
    selector: '[context-link]'
})
export class LinkWithContextDirective implements OnInit, OnDestroy, AfterViewChecked {
    contextElement: HTMLDivElement;
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
        console.log($event, type);
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
    }

    removeElement() {
        const elements = document.getElementsByClassName('link-context-menu');
        while (elements.length > 0) {
            elements[0].parentNode.removeChild(elements[0]);
        }
    }
}