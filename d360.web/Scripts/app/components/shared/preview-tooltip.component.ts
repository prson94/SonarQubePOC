import { Component, EventEmitter, Output, Input, HostBinding, ChangeDetectionStrategy, OnInit, ChangeDetectorRef } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { ToolTipService } from '../../services/tooltip.service';
import { TooltipInfo, TooltipFieldValue } from '../../models/tooltip-info.model';
import { TooltipSingletonService } from '../../services/tooltip-singleton.service';

@Component({
    selector: 'd3s-preview-tooltip',
    templateUrl:'./preview-tooltip.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [ToolTipService] 
})

export class PreviewTooltipComponent  {        
    @Input() objectType: string;
    @Input() objectId: number;
    @Input() icon: string;
    @Input() class: string;
    @Input() innerHtmlContent: string;
    @HostBinding('style.color') @Input() iconColor: string;
    @HostBinding('style.background') @Input() foreColor: string;

    private hideHandle: number = 0;
    private showHandle: number = 0;
    public active: boolean = false;
    public data: TooltipInfo = null;
    private toolTipSub: any;

    @Output() click = new EventEmitter();    

    constructor(private toolTipService: ToolTipService,
        private router: Router,
        protected tooltipSingletonService: TooltipSingletonService,
        private ref: ChangeDetectorRef) {
        this.toolTipSub = this.tooltipSingletonService.tooltipMessage$.subscribe(
            info => {
                if (info.objectId == this.objectId && info.objectType == this.objectType) return;
                this.hide();
            });
    }

    ngOnDestroy() {
        this.toolTipSub.unsubscribe();
    }

    private load(item, tip) {
        this.tooltipSingletonService.tooltipShow(this.objectType, this.objectId);
        if (!this.data) {
            //get object properties for the tooltip
            this.toolTipService.getTooltipInfo(this.objectType, this.objectId).then(res => {
                if (!res.ShowTooltip) {
                    this.active = false;
                    return;
                }
                this.data = res;
                this.showPanel(tip, item);
                this.ref.markForCheck();
            });
        }
        else {
            this.showPanel(tip, item);
            this.ref.markForCheck();
        }
    }

    private formattedUrl(url: string): string {
        if (url != null && !url.startsWith("/"))
            return "/" + url;
        else
            return url;
    }

    show(item, tip) {        
        if (this.showHandle > 0) return; //pending show ignore new request
        // check for any pending hides and cancel them
        if (this.hideHandle > 0) {
            window.clearTimeout(this.hideHandle);
            this.hideHandle = 0;
        }

        this.showHandle = window.setTimeout(() => {
            this.load(item, tip);
            this.showHandle = 0;
        },
            100);
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
            panel.style.left = item.getBoundingClientRect().left + 'px';;
            
            window.setTimeout(() => {
                this.repositionMenuToFit(window.innerHeight, window.innerWidth, panel);
            }, 50);
        }
    }

    hide() {        
        if (this.hideHandle > 0) return; //pending hide ignore new request
        //queue up a request to hide the window.
        // check for any pending hides and cancel them
        if (this.showHandle > 0) {
            window.clearTimeout(this.showHandle);
            this.showHandle = 0;
        }

        this.hideHandle = window.setTimeout(() => {
            this.active = false;            
            this.hideHandle = 0;
            this.ref.markForCheck();
        },
            40);
    }

};
