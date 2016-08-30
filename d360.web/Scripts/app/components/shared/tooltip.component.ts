import { Component, EventEmitter, Output, Input, HostBinding } from '@angular/core';

@Component({
    selector: 'd3s-tooltip',
    template: `                 
                <a *ngIf="icon && icon !=''" [attr.data-type]="objectType" [attr.data-context]="tooltipType" [attr.data-id]="objectId" (click)="click.emit()" data-hasqtip="true" aria-describedby="qtip-1"><i class="fa" [ngClass]="getIconName()"  [ngStyle]="{'color': iconColor}"></i></a>
                <div *ngIf="icon == null || icon ==''"  style="display: inline-block;" [attr.data-type]="objectType" [attr.data-context]="tooltipType" [attr.data-id]="objectId" (click)="click.emit()" data-hasqtip="true" aria-describedby="qtip-1">
                    <ng-content></ng-content>
                </div>
              `
})

export class TooltipComponent  {    
    @Input() tooltipType: string; // preview, certificate etc;
    @Input() objectType: string;
    @Input() objectId: number;
    @Input() icon: string;
    @HostBinding('style.color') @Input() iconColor: string;
    @HostBinding('style.background') @Input() foreColor: string;

    @Output() click = new EventEmitter();    


    private getIconName(): string{
        return 'fa-' + this.icon;
    }
};
