import { Component, EventEmitter, Output, Input, HostBinding, ChangeDetectionStrategy, OnInit, ChangeDetectorRef } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { ToolTipService } from '../../services/tooltip.service';
import { TooltipInfo, TooltipFieldValue } from '../../models/tooltip-info.model';

@Component({
    selector: 'd3s-preview-tooltip',
    template: ` 
                <span #item style="overflow:visible;" class="d3s-tooltip" [ngClass]="{'d3s-tooltip-active':active}" (mouseenter)="show(item)" (mouseleave)="hide(item)" (click)="click.emit()">
                    <i *ngIf="icon && icon !=''" class="fa" [ngClass]="['fa-' + this.icon, class ? class: '']"  [ngStyle]="{'color': iconColor}"></i>                    
                    <ng-content></ng-content>                    
                    <div class="tooltip-child tooltip-panel">
                        <h3 style="positon: relative"><a [routerLink]="data?.Url">{{data?.DisplayName}}</a> <small *ngIf="data && data.TypeName" style="background-color: #fff; float:right;font-size:65%;">{{data.TypeName}}</small></h3>
                        <div>&nbsp;</div>
                        <p *ngIf="data?.Description" [innerHtml]="data?.Description"></p>
                        <div *ngFor="let field of data?.FieldValues"><span *ngIf="field.Value"><b>{{field.Name}}</b>: <span [innerHtml]="field.Value"></span></span></div>                        
                    </div>
                </span>
              `,
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [ToolTipService]
})

export class PreviewTooltipComponent  {        
    @Input() objectType: string;
    @Input() objectId: number;
    @Input() icon: string;
    @Input() class: string;
    @HostBinding('style.color') @Input() iconColor: string;
    @HostBinding('style.background') @Input() foreColor: string;

    private hideHandle: number = 0;
    private active: boolean = false;
    private data: TooltipInfo = null;
    

    @Output() click = new EventEmitter();    

    constructor(private toolTipService: ToolTipService,
        private router: Router,
        private ref: ChangeDetectorRef) {
    }
    

    private load(item) {
        if (!this.data) {
            //get object properties for the tooltip
            this.toolTipService.getTooltipInfo(this.objectType, this.objectId).then(res => {
                this.data = res;
                this.showPanel(item.children[0].nextElementSibling, item);
                this.ref.markForCheck();
            });
        }
        else {
            this.showPanel(item.children[0].nextElementSibling, item);
        }
    }

    show(item) {
        // check for any pending hides and cancel them
        if (this.hideHandle > 0) {
            window.clearTimeout(this.hideHandle);
            this.hideHandle = 0;
        }
        
        this.load(item);
        
    }

    showPanel(panel,item) {
        if (panel) {
            this.active = true;
            panel.style.zIndex = 1000;
            panel.style.top = (item.offsetHeight - 1) + 'px'; // -1 for the border so it blends
            panel.style.right = '0px';
        }
    }

    hide(item) {
        if (this.hideHandle > 0) return; //pending hide ignore new request
        //queue up a request to hide the window.
        this.hideHandle = window.setTimeout(() => {
            this.active = false;
            this.ref.markForCheck();
        },
            500);
    }

};
