import { Component, EventEmitter, Output, Input, HostBinding, ChangeDetectionStrategy, OnInit, ChangeDetectorRef } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { ToolTipService } from '../../services/tooltip.service';
import { TooltipInfo, TooltipFieldValue, LookupTooltipInfo } from '../../models/tooltip-info.model';

@Component({
    selector: 'd3s-lookup-tooltip',
    template: ` 
                <span #item class="d3s-tooltip" [ngClass]="{'d3s-tooltip-active':active}" (mouseenter)="show(item,tip)" (mouseleave)="hide(item)" (click)="click.emit()">
                    <i *ngIf="icon && icon !=''" class="fa" [ngClass]="['fa-' + this.icon, class ? class: '']"  [ngStyle]="{'color': iconColor}"></i>                    
                    <ng-content></ng-content>                    
                    <div class="tooltip-child tooltip-panel" #tip [innerHtml]="data?.html"></div>
                </span>
              `,
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [ToolTipService]
})

export class LookupTooltipComponent  {        
    @Input() objectType: string;
    @Input() objectId: number;
    @Input() icon: string;
    @Input() class: string;
    @HostBinding('style.color') @Input() iconColor: string;
    @HostBinding('style.background') @Input() foreColor: string;

    private hideHandle: number = 0;
    public active: boolean = false;
    public data: LookupTooltipInfo = null;
    

    @Output() click = new EventEmitter();    

    constructor(private toolTipService: ToolTipService,
        private router: Router,
        private ref: ChangeDetectorRef) {
    }
    

    private load(item,tip) {
        if (!this.data) {
            //get object properties for the tooltip
            this.toolTipService.getLookupTooltipInfo(this.objectType, this.objectId).then(res => {
                this.data = res;
                this.showPanel(tip, item);
                this.ref.markForCheck();
            });
        }
        else {
            this.showPanel(tip, item);
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

    show(item,tip) {
        // check for any pending hides and cancel them
        if (this.hideHandle > 0) {
            window.clearTimeout(this.hideHandle);
            this.hideHandle = 0;
        }
        this.load(item,tip);
    }

    hide(item) {
        if (this.hideHandle > 0) return; //pending hide ignore new request
        //queue up a request to hide the window.
        this.hideHandle = window.setTimeout(() => {
            this.active = false;
            this.ref.markForCheck();
        },
            200);
    }
};