/*
    Infogix UI Component Library Button component as defined http://prototype.infogix.com/primeng/buttondemo
    Sourced from https://github.com/Infogix/styleguide-primeng/blob/master/src/app/components/button/button.ts
    DomUtils replaced by DomHandler for primefaces 7.1.3 compatability
*/

import { NgModule, Directive, ElementRef, AfterViewInit, OnDestroy, Input } from '@angular/core';
import { DomHandler } from 'primeng/dom';
import { CommonModule } from '@angular/common';

@Directive({
    selector: '[igButton]'
})
export class ButtonDirective implements AfterViewInit, OnDestroy {
    @Input() tooltip: string;
    @Input() darkMode: boolean = false;

    public _label: string;
    public _icon: string;
    public _loading: boolean;

    constructor(public el: ElementRef) { }

    ngAfterViewInit() {
        DomHandler.addMultipleClasses(this.el.nativeElement, this.getStyleClass());

        if (this.tooltip) {
            this.el.nativeElement.setAttribute("title", this.tooltip);
            this.el.nativeElement.setAttribute("aria-label", this.tooltip);
        }
    }

    getStyleClass(): string {
        let styleClass = 'ig-button';
        if (!this.label) {
            styleClass = styleClass + ' ig-button-icon-only';
        }
        if (this.darkMode) {
            styleClass += ' ig-button-dark';
        }
        return styleClass;
    }

    @Input() get label(): string {
        return this._label;
    }

    set label(val: string) {
        this._label = val;

        let labelElement = DomHandler.findSingle(this.el.nativeElement, '.ig-button-label');
        if (labelElement) {
            this.el.nativeElement.removeChild(labelElement);
        }

        if (this._label) {
            labelElement = document.createElement("span");
            labelElement.className = 'ig-button-label';
            labelElement.appendChild(document.createTextNode(this.label));
            this.el.nativeElement.appendChild(labelElement);
            DomHandler.removeClass(this.el.nativeElement, "ig-button-icon-only");
        } else {
            DomHandler.addClass(this.el.nativeElement, "ig-button-icon-only");
            throw new Error("Infogix Button Component: caption has not been set");
        }
    }

    @Input() get icon(): string {
        return this._icon;
    }

    set icon(val: string) {
        this._icon = val;

        let iconElement = DomHandler.findSingle(this.el.nativeElement, '.ig-button-icon');
        if (iconElement) {
            this.el.nativeElement.removeChild(iconElement);
        }

        if (this._icon) {
            let iconElement = document.createElement("span");
            iconElement.setAttribute("aria-hidden", "true");
            iconElement.className = 'ig-button-icon fa ' + this._icon;

            let labelElement = DomHandler.findSingle(this.el.nativeElement, '.ig-button-label');
            if (labelElement) {
                this.el.nativeElement.insertBefore(iconElement, labelElement);
            } else {
                this.el.nativeElement.appendChild(iconElement);
            }
        }
    }

    @Input() get loading(): boolean {
        return this._loading;
    }

    set loading(val: boolean) {
        this._loading = val;

        let spinnerElement = DomHandler.findSingle(this.el.nativeElement, '.ig-button-spinner');
        if (spinnerElement) {
            this.el.nativeElement.removeChild(spinnerElement);
        }

        if (this._loading) {
            DomHandler.addClass(this.el.nativeElement, "ig-state-loading");

            let spinnerElement = document.createElement("span");
            spinnerElement.setAttribute("aria-hidden", "true");
            spinnerElement.className = 'ig-button-spinner';
            this.el.nativeElement.appendChild(spinnerElement);
        } else {
            DomHandler.removeClass(this.el.nativeElement, "ig-state-loading");
        }
    }

    ngOnDestroy() {
        while (this.el.nativeElement.hasChildNodes()) {
            this.el.nativeElement.removeChild(this.el.nativeElement.lastChild);
        }
    }
}

@NgModule({
    imports: [CommonModule],
    exports: [ButtonDirective],
    declarations: [ButtonDirective]
})
export class ButtonModule { }