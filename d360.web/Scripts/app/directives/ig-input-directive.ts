import { NgModule, Directive, ElementRef, AfterViewInit, OnDestroy, Input } from '@angular/core';
import { DomHandler } from 'primeng/dom';
import { CommonModule } from '@angular/common';

@Directive({
    selector: '[igInput]'
})
export class InputDirective implements AfterViewInit, OnDestroy {

    @Input() tooltip: string;

    public _label: string;
    public _icon: string;
    public _istextarea: boolean;
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
        let styleClass = 'ig-input';
        if (!this.label) {
            styleClass = styleClass + ' ig-input';
        }
        return styleClass;
    }

    @Input() get label(): string {
        return this._label;
    }

    set label(val: string) {
        this._label = val;

        let labelElement = DomHandler.findSingle(this.el.nativeElement, '.ig-input-label');
        if (labelElement) {
            this.el.nativeElement.removeChild(labelElement);
        }

        if (this._label) {
            labelElement = document.createElement("span");
            labelElement.className = 'ig-input-label';
            labelElement.appendChild(document.createTextNode(this.label));
            this.el.nativeElement.parentNode.insertBefore(labelElement, this.el.nativeElement);
            DomHandler.removeClass(this.el.nativeElement, "ig-input-icon-only");
        } else {
            DomHandler.addClass(this.el.nativeElement, "ig-input-icon-only");
            throw new Error("Infogix Button Component: caption has not been set");
        }
    }

    @Input() get icon(): string {
        return this._icon;
    }

    set icon(val: string) {
        this._icon = val;

        let iconElement = DomHandler.findSingle(this.el.nativeElement, '.ig-input-icon');
        if (iconElement) {
            this.el.nativeElement.removeChild(iconElement);
        }

        if (this._icon) {
            let iconElement = document.createElement("span");
            iconElement.setAttribute("aria-hidden", "true");
            iconElement.className = 'ig-input-icon fa ' + this._icon;

            let labelElement = DomHandler.findSingle(this.el.nativeElement, '.ig-input-label');
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

        let spinnerElement = DomHandler.findSingle(this.el.nativeElement, '.ig-input-spinner');
        if (spinnerElement) {
            this.el.nativeElement.removeChild(spinnerElement);
        }

        if (this._loading) {
            DomHandler.addClass(this.el.nativeElement, "ig-state-loading");
            this.el.nativeElement.setAttribute("disabled", "true");

            let spinnerElement = document.createElement("span");
            spinnerElement.setAttribute("aria-hidden", "true");
            spinnerElement.className = 'ig-input-spinner';
            this.el.nativeElement.appendChild(spinnerElement);
        } else {
            DomHandler.removeClass(this.el.nativeElement, "ig-state-loading");
            this.el.nativeElement.setAttribute("disabled", "false");
        }
    }

    @Input() get istextarea(): boolean {
        return this._istextarea;
    }

    set istextarea(val: boolean) {
        this._istextarea = val;
        if (this._istextarea) {
            DomHandler.addMultipleClasses(this.el.nativeElement, "text-area");
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
    exports: [InputDirective],
    declarations: [InputDirective]
})
export class InputModule { }