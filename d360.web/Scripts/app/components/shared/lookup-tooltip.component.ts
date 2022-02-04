import { Component, EventEmitter, Output, Input, HostBinding, ChangeDetectionStrategy, OnInit, ChangeDetectorRef, OnDestroy, ViewChild, ElementRef } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { ToolTipService } from '../../services/tooltip.service';
import { TooltipSingletonService } from '../../services/tooltip-singleton.service';
import { TooltipInfo, TooltipFieldValue, LookupTooltipInfo } from '../../models/tooltip-info.model';

@Component({
    selector: 'd3s-lookup-tooltip',
    template: ` 
                <span #item class="d3s-tooltip" [ngClass]="{'d3s-tooltip-active':active}" (mouseenter)="show(item,tip)" (mouseleave)="hide()" (click)="click.emit()">
                    <i *ngIf="icon && icon !=''" class="fa" [ngClass]="['fa-' + this.icon, class ? class: '']"  [ngStyle]="{'color': iconColor}"></i>
                    <div style="display: inline-block" #lookupText>
                        <ng-content></ng-content>   
                    </div>                
                    <div class="tooltip-child tooltip-panel" #tip [innerHtml]="data?.html"></div>
                </span>
              `,
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [ToolTipService]
})

export class LookupTooltipComponent implements OnDestroy  {        
    @Input() objectType: string;
    @Input() objectId: number;
    @Input() icon: string;
    @Input() class: string;
    @Input() contentAnchor: string = 'left';
    @Input() field: any;
    @HostBinding('style.color') @Input() iconColor: string;
    @HostBinding('style.background') @Input() foreColor: string;

    private hideHandle: number = 0;
    private showHandle: number = 0;
    public active: boolean = false;
    public data: LookupTooltipInfo = null;
    private toolTipSub: any;

    @ViewChild('lookupText') lookupText: ElementRef;

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
        if (this.toolTipSub) {
            this.toolTipSub.unsubscribe();
        }
    }
    
    private load(item, tip) {
        if (this.field && this.field.HideTooltip) {
            return;
        }
        this.tooltipSingletonService.tooltipShow(this.objectType, this.objectId);
        if (!this.data) {
            //get object properties for the tooltip
            this.toolTipService.getLookupTooltipInfo(this.objectType, this.objectId)
                .subscribe(res => {
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
    
    showPanel(panel, item) {
        let xoffset = 0;
        if (this.contentAnchor === 'right' && this.lookupText && this.lookupText.nativeElement) {
            xoffset = this.lookupText.nativeElement.offsetWidth + 5;
        }
        if (panel && !this.active) {            
            this.active = true;
            panel.style.zIndex = 1000;
            panel.style.top = item.getBoundingClientRect().bottom + 'px';
            panel.style.left = xoffset + item.getBoundingClientRect().left + 'px';
                        
            window.setTimeout(() => {
                this.repositionMenuToFit(window.innerHeight, window.innerWidth, panel);
            }, 50);
        }
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

    show(item, tip) {
        if (this.showHandle > 0) return; //pending show ignore new request
        // check for any pending hides and cancel them
        if (this.hideHandle > 0) {
            window.clearTimeout(this.hideHandle);
            this.hideHandle = 0;
        }

        this.showHandle = window.setTimeout(() => {
            this.load(item, tip);
            //send message to service to close any other open tooltips
            this.showHandle = 0;
        },
            100);
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
}