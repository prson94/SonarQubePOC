import { Input, Component, OnInit, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { DetailRow, DetailField, DetailModel, DetailFieldType, DetailSubField } from '../../../models/object-detail.model';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { Router } from '@angular/router';

@Component({
    selector: 'object-detail-field',
    template: `
            <div *ngIf="field.Values && field.Values.length > 0">
                <div *ngFor="let item of field.Values">
                    <d3s-tooltip [tooltipType]="item.TooltipContext" [objectType]="item.TooltipType" [objectId]="item.TooltipID">
                        <a (click)="navigate(item.TooltipUrl)" [innerHtml]="item.Value"></a>
                    </d3s-tooltip>
                </div>
            </div>            
            <ng-template [ngIf]="!field.Values || field.Values.length == 0">
                <div *ngIf="field.Type == DetailFieldType.Field && field.Name == 'Email'" class="FieldDisplayContent"><a [href]="'mailto:' + field.Value">{{field.Value}}</a></div>
                <div *ngIf="field.Type == DetailFieldType.Field && field.Name != 'Email' && field.DataType == 'date'" class="FieldDisplayContent" [innerHtml]="field.Value | date:'shortDate'"></div>
                <div *ngIf="field.Type == DetailFieldType.Field && field.Name != 'Email' && field.DataType != 'date'" class="FieldDisplayContent" [innerHtml]="field.Value"></div>
                <div *ngIf="field.Type == DetailFieldType.Tooltip" class="FieldDisplayContent">
                    <d3s-tooltip [tooltipType]="field.TooltipContext" [objectType]="field.TooltipType" [objectId]="field.TooltipID">
                        <a (click)="navigate(field.TooltipUrl)" [innerHtml]="field.Value"></a>
                    </d3s-tooltip>
                </div>
                <div *ngIf="field.Type == DetailFieldType.Lookup">
                    <d3s-dynamic-lookup-grid *ngIf="field.Data && field.Data.Values && field.Data.Values.length > 0" [data]="field.Data" [hideHeader]="field.HideHeader" [hideFooter]="field.HideFooter" [hideFilter]="field.HideFilter"></d3s-dynamic-lookup-grid>
                </div>
            </ng-template>
    `
})

export class ObjectDetailFieldComponent {
    @Input() field: DetailField;
    DetailFieldType = DetailFieldType;

    constructor(private router: Router) {}

    navigate(url: string) {        
        this.router.navigateByUrl(SiteUrlHelpers.convertClassicUrl(url));
    }    
}

