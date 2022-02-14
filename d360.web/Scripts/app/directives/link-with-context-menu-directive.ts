import { DOCUMENT } from '@angular/common';
import { AfterViewChecked, OnDestroy } from '@angular/core';
import { Inject, OnInit, Renderer2 } from '@angular/core';
import { Directive, ElementRef, HostListener } from '@angular/core';
import { DomHandler } from 'primeng/dom';
import { CompanySettingEnum } from '../models/settings.model';
import { AuthenticationService } from '../services/authentication.service';
import { CompanySettingsService } from '../services/settings.service';

@Directive({
    selector: '[context-link]'
})
export class LinkWithContextDirective implements OnInit, OnDestroy, AfterViewChecked {
    contextElement: HTMLDivElement;
    hoverElement: HTMLDivElement;
    hoverTooltipWidth: number = 350;

    private isTagTooltip: boolean = false;
    isAdmin: boolean = false;

    contextMenuItems: any[] = [
        { title: 'View Information', value: 'info' },
        { title: 'Open', value: 'open' },
        { title: 'Open in New Tab', value: 'new-tab' }
    ]

    canViewUsers: boolean = true;

    constructor(private el: ElementRef,
        private authenticationService: AuthenticationService,
        private settingsService: CompanySettingsService,
        @Inject(DOCUMENT) private document: Document,
        private renderer: Renderer2) {
    }

    ngOnInit() {
        var htmlEl = this.el.nativeElement as HTMLElement;
        this.canViewUsers = this.authenticationService.isAdmin || this.settingsService.getSettingById(CompanySettingEnum.ShowResources).BooleanSetting.Value;

        if (this.isLinkDisabled) {
            htmlEl.style.pointerEvents = "none";
            htmlEl.classList.add("disabled");
        }
        else {
            htmlEl.onmouseenter = () => {
                this.addTooltip();
            };

            htmlEl.onmouseleave = () => {
                this.removeTooltip();
            };
            DomHandler.addClass(htmlEl, 'has-context');
        }
    }

    get isLinkDisabled(): boolean {
        return this.isLinkToResource && !this.canViewUsers;
    }

    addTooltip() {
        this.hoverElement = this.document.createElement('div');
        this.hoverElement.style.display = "block";
        this.hoverElement.style.position = "fixed";
        this.hoverElement.style.width = this.hoverTooltipWidth + "px";

        this.renderer.setAttribute(this.hoverElement, 'class', 'link-context-menu-p-tooltip p-tooltip p-component p-tooltip-top ig-tooltip');
        var isTag = typeof this.el.nativeElement.dataset['tooltip'] !== "undefined";

        this.setTooltipValue(isTag);
        this.renderer.appendChild(this.document.body, this.hoverElement);
        this.updatePosition();
    }

    setTooltipValue(isInitialTag: boolean) {
        this.hoverElement.innerHTML = "";
        var hoverItem = this.document.createElement('div');
        this.renderer.setAttribute(hoverItem, 'class', 'p-tooltip-text');

        var value = this.el.nativeElement.dataset['tooltip'] as string;
        var refElement = "link";
        var html = "";
        if (value) {
            html += value + "<span style='display:block; height:8px;'></span>";
            this.isTagTooltip = true;
            refElement = "tag";

            if (value.indexOf('spinner') > -1) {
                isInitialTag = true;
            }
        }
        html += "Click the " + refElement + " to view information in the side panel or right-click for more options";

        if (isInitialTag) {
            html = `<i style="margin-left: 44%;" class="fa fa-spinner fa-spin fa-2x"></i>`;
            setTimeout(() => this.setTooltipValue(false), 500);
        }

        hoverItem.innerHTML = html;
        this.hoverElement.appendChild(hoverItem);
    }

    get isLinkToResource(): boolean {
        return this.el.nativeElement?.dataset?.linkType === "resource";
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
        var box;
        if (htmlEl && this.contextElement) {
            box = htmlEl.getBoundingClientRect();
            this.contextElement.style.top = (box.top + box.height) + "px";
            this.contextElement.style.left = box.left + "px";
        }

        if (htmlEl && this.hoverElement) {
            box = htmlEl.getBoundingClientRect();
            this.hoverElement.style.top = (box.top - this.hoverElement.getBoundingClientRect().height) + "px";

            if (this.isTagTooltip) {
                this.hoverElement.style.top = (box.top - 86) + "px";
            }

            //update leftPosition if its calculated value is outside bounds of the browser
            var leftPosition = (box.left + (box.width / 2) - (this.hoverTooltipWidth / 2));
            if (leftPosition < 5) {
                leftPosition = 5;
            }

            var caluclatedRightPosition = leftPosition + this.hoverTooltipWidth;
            if (caluclatedRightPosition > window.outerWidth) {
                leftPosition = leftPosition - (caluclatedRightPosition - window.outerWidth) - 5;
            }

            this.hoverElement.style.left = leftPosition + "px";
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
