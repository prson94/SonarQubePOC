import { Component, EventEmitter, Output, Input } from '@angular/core';

@Component({
    selector: 'd3s-tooltip',
    template: `                 
                <a [attr.data-type]="objectType" [attr.data-context]="tooltipType" [attr.data-id]="objectId" (click)="click.emit()" data-hasqtip="true" aria-describedby="qtip-1"><i class="fa" [ngClass]="getIconName()"  [ngStyle]="{'color': iconColor}"></i></a>
              `
})

export class TooltipComponent  {    
    @Input() tooltipType: string; // preview, certificate etc;
    @Input() objectType: string;
    @Input() objectId: number;
    @Input() icon: string;
    @Input() iconColor: string;

    @Output() click = new EventEmitter();    


    private getIconName(): string{
        return 'fa-' + this.icon;
    }
};
