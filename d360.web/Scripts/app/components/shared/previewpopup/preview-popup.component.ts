import { ChangeDetectionStrategy, ChangeDetectorRef, Component, ElementRef, EventEmitter, Input, Output, SimpleChange, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Subject, Subscription } from "rxjs";
import { TooltipInfo } from '../../../models/tooltip-info.model';
import { PreviewpopupSingletonService } from '../../../services/previewpopup-singleton.service';
import { ToolTipService } from '../../../services/tooltip.service';
import { DomHandler } from 'primeng/dom';


@Component({
    selector: 'd3s-preview-popup',
    templateUrl: './preview-popup.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [ToolTipService]
})

export class PreviewPopupComponent implements OnInit {
    @Input() icon: string;
    @Input() class: string;
    @Input() uid: string;
    @Input() seed: TooltipInfo = null;

    private _display: boolean = false;
    displaydialog: boolean = false;

    @Input() get display(): any {
        return this._display;
    }

    set display(value: any) {
        this._display = value;
        if (this._display && !this.displaydialog) {
            this.displaydialog = true;
            this.showpreview();
        }
    }
    @Output() displayChange: EventEmitter<any> = new EventEmitter();

    public active: boolean = false;
    public loading: boolean = false;

    private remoteAugment: boolean = false;
    data: TooltipInfo;

    private subscriptions: Subscription = new Subscription();

    public hideDebounce: Subject<any> = new Subject();

    constructor(
        private toolTipService: ToolTipService,
        private router: Router,
        protected popupSingletonService: PreviewpopupSingletonService,
        private ref: ChangeDetectorRef,
        private elRef: ElementRef
    ) {
        this.popupSingletonService.popupMessage$.subscribe(
            info => {
                if (info.uid == this.uid || !this.displaydialog) return;
                setTimeout(() => {
                    this.closeDialog();
                }, 10);
                
            });
    }

    ngOnInit() {
        if (this.data == undefined && this.seed != undefined) {
            this.data = this.seed;
        }
    }

    ngOnDestroy() {
        if (this.subscriptions) {
            this.subscriptions.unsubscribe();
        }
    }

    private showpreview() {
        if (!this.remoteAugment) {
            this.loading = true;
            this.remoteAugment = true;

            //get object properties for the tooltip
            if (this.uid) {
                this.toolTipService.getTooltipInfoByUid(this.uid)
                    .subscribe(res => {
                        this.data = res;
                        this.loading = false;
                        this.ref.markForCheck();
                    });
            }
        }
        this.popupSingletonService.popupShow(this.uid);
    }

    private formattedUrl(url: string): string {
        if (url != null && !url.startsWith("/"))
            return "/" + url;
        else
            return url;
    }

    getLeft() {
        return this.elRef.nativeElement.offsetLeft - (468+10);
    }

    getTop(): number {
        var viewport = DomHandler.getViewport();
        return Math.min(this.elRef.nativeElement.offsetTop, (viewport.height/2)-30);
    }

    repositionMenuToFit(windowHeight, windowWidth, element) {
        var dims = element.getBoundingClientRect();

        if (dims) {
            var maxHeight = dims.top + dims.height;
            var maxWidth = dims.left + dims.width;

            if (maxHeight > windowHeight) { //case where bottom is below page
                var topOffset = windowHeight - dims.height - 10;
                element.style.top = topOffset + 'px';
            }

            if (maxWidth > windowWidth) {
                var leftOffset = windowWidth - dims.width - 30;
                element.style.left = leftOffset + 'px';
            }
        }
    }

    showPanel(panel, item) {
        if (panel && !this.active) {
            this.active = true;
            panel.style.zIndex = 1000;
            panel.style.top = item.getBoundingClientRect().bottom + 'px';
            panel.style.left = item.getBoundingClientRect().left + 'px';

            window.setTimeout(() => {
                this.repositionMenuToFit(window.innerHeight, window.innerWidth, panel);
            }, 50);
        }
    }

    private GetJSON(value: string) {
        try {
            return JSON.parse(value);
        } catch (err) {
            return "NULL";
        }
    }

    closeDialog() {
        if (this.displaydialog) {
            this.displaydialog = false;
            this.displayChange.emit(false);
        }
    }

}
