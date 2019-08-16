import { Component, EventEmitter, Output, Input, HostBinding, ChangeDetectionStrategy } from '@angular/core';

@Component({
    selector: 'd3s-tooltip',
    template: `                 
                <a *ngIf="icon && icon !=''" [attr.data-type]="objectType" [attr.data-context]="tooltipType" [attr.data-id]="objectId" (click)="click.emit()"><i class="fa" [ngClass]="['fa-' + this.icon, class ? class: '']"  [ngStyle]="{'color': iconColor}"></i></a>
                <div *ngIf="icon == null || icon ==''"  style="display: inline-block;" [attr.data-type]="objectType" [attr.data-context]="tooltipType" [attr.data-id]="objectId" (click)="click.emit()">
                    <ng-content></ng-content>
                </div>
              `,
    changeDetection: ChangeDetectionStrategy.OnPush    
})

export class TooltipComponent  {    
    @Input() tooltipType: string; // preview, certificate etc;
    @Input() objectType: string;
    @Input() objectId: number;
    @Input() icon: string;
    @Input() class: string;
    @HostBinding('style.color') @Input() iconColor: string;
    @HostBinding('style.background') @Input() foreColor: string;

    @Output() click = new EventEmitter();        
};
