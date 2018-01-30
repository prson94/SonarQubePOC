import { Input, Component, OnInit, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { DetailRow, DetailField, DetailModel, DetailFieldType, DetailSubField } from '../../../models/object-detail.model';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { Router } from '@angular/router';

@Component({
    selector: 'object-detail-field',
    template: `
            <div *ngIf="field.Values && field.Values.length > 0">
                <div *ngFor="let item of field.Values">                    
                    <d3s-preview-tooltip [objectType]="item.TooltipType" [objectId]="item.TooltipID">
                        <a *ngIf="item.TooltipUrl;else arrayNoUrl" (click)="navigate(item.TooltipUrl)" [innerHtml]="item.Value"></a>
                        <ng-template #arrayNoUrl>
                            <span [innerHtml]="item.Value"></span>
                        </ng-template>
                    </d3s-preview-tooltip>
                </div>
            </div>            
            <ng-template [ngIf]="!field.Values || field.Values.length == 0">
                <div *ngIf="field.Type == DetailFieldType.Field && field.Name == 'Email'" class="FieldDisplayContent"><a [href]="'mailto:' + field.Value">{{field.Value}}</a></div>
                <div *ngIf="field.Type == DetailFieldType.Field && field.Name != 'Email' && field.DataType == 'date'" class="FieldDisplayContent" [innerHtml]="field.Value | date:'shortDate'"></div>
                <div *ngIf="field.Type == DetailFieldType.Field && field.Name != 'Email' && field.DataType == 'text'" class="FieldDisplayContent">{{field.Value}}</div>
                <div *ngIf="field.Type == DetailFieldType.Field && field.Name != 'Email' && field.DataType == 'bool'" class="FieldDisplayContent">                    
                    <i *ngIf="(field.Value || '').toUpperCase() == 'TRUE'" class="fa fa-check enabled" title="True"></i>
                    <i *ngIf="(field.Value || '').toUpperCase() == 'FALSE'" class="fa fa-times disabled" title="False"></i>                
                </div>
                <div *ngIf="field.Type == DetailFieldType.Field && field.Name != 'Email' && field.DataType != 'date' && field.DataType != 'text' && field.DataType != 'bool'" class="FieldDisplayContent" [innerHtml]="field.Value" [ngStyle]="{'font-weight':(field.Name == 'Name' ? 'bold':'')}"></div>
                <div *ngIf="field.Type == DetailFieldType.Tooltip" class="FieldDisplayContent">                    
                    <d3s-lookup-tooltip *ngIf="field.TooltipUrl;else noLinkTooltip" [objectType]="field.TooltipType" [objectId]="field.TooltipID">
                        <a (click)="navigate(field.TooltipUrl)" [innerHtml]="field.Value"></a>                        
                    </d3s-lookup-tooltip>
                    <ng-template #noLinkTooltip>
                        <d3s-lookup-tooltip  [objectType]="field.TooltipType" [objectId]="field.TooltipID">
                            <span [innerHtml]="field.Value"></span>
                        </d3s-lookup-tooltip>
                    </ng-template>                    
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

