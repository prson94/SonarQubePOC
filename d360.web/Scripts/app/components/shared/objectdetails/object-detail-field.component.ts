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
            <style>
            a,
            span,
            div {
                word-break: break-all;
            }
        </style>
                <div *ngIf="field.Type == DetailFieldType.Field && field.Name == 'Email'" class="FieldDisplayContent"><a [href]="'mailto:' + field.Value">{{field.Value}}</a></div>
                <div *ngIf="field.Type == DetailFieldType.Field && field.Name != 'Email' && field.DataType == 'date'" class="FieldDisplayContent" >
                    <div *ngIf="field.Value" [innerHtml]="field.Value | date:'shortDate'"></div>
                </div>
                <div *ngIf="field.Type == DetailFieldType.Field && field.Name != 'Email' && field.DataType == 'text'" class="FieldDisplayContent">{{field.Value}}</div>
                <div *ngIf="field.Type == DetailFieldType.Field && field.Name != 'Email' && field.DataType == 'bool'" class="FieldDisplayContent">                    
                    <i *ngIf="(field.Value || '').toUpperCase() == 'TRUE'" class="fa fa-check enabled" title="True"></i>
                    <i *ngIf="(field.Value || '').toUpperCase() == 'FALSE'" class="fa fa-times disabled" title="False"></i>                
                </div>
                <div *ngIf="field.Type == DetailFieldType.Field && field.Name != 'Email' && field.DataType != 'date' && field.DataType != 'text' && field.DataType != 'bool'" class="FieldDisplayContent" [innerHtml]="field.Value | safeHtml" [ngStyle]="{'font-weight':(field.Name == 'Name' ? 'bold':'')}"></div>            
                <div *ngIf="field.Type == DetailFieldType.Tooltip" class="FieldDisplayContent">                                        
                    <d3s-preview-tooltip *ngIf="field.TooltipContext == 'Preview';else listTooltip"  [objectType]="field.TooltipType" [objectId]="field.TooltipID">
                        <a (click)="navigate(field.TooltipUrl)">{{field.Value}}</a>                        
                    </d3s-preview-tooltip>
                    <ng-template #listTooltip>
                        <d3s-lookup-tooltip *ngIf="field.TooltipUrl;else noLinkTooltip" [objectType]="field.TooltipType" [objectId]="field.TooltipID">
                            <a (click)="navigate(field.TooltipUrl)" [innerText]="field.Value"></a>                        
                        </d3s-lookup-tooltip>
                        <ng-template #noLinkTooltip>
                            <d3s-lookup-tooltip  [objectType]="field.TooltipType" [objectId]="field.TooltipID">
                                <span [innerText]="field.Value"></span>
                            </d3s-lookup-tooltip>
                        </ng-template>                    
                    </ng-template>
                </div>
                <div *ngIf="field.Type == DetailFieldType.Lookup">
                    <d3s-dynamic-lookup-grid *ngIf="field.Data && field.Data.Values && field.Data.Values.length > 0" [data]="field.Data" [hideHeader]="field.HideHeader" [hideFooter]="field.HideFooter" [hideFilter]="field.HideFilter" [field]="field"></d3s-dynamic-lookup-grid>
                </div>
            </ng-template>
    `
})

export class ObjectDetailFieldComponent {
    @Input() field: DetailField;
    DetailFieldType = DetailFieldType;

    constructor(private router: Router) {}
    ngOnInit() {

          if ((this.field.DataType == 'date' || this.field.DataType == 'datetime') && isNaN(Date.parse(this.field.Value)))
                this.field.Value = null;
    }

    navigate(url: string) {        
        this.router.navigateByUrl(SiteUrlHelpers.convertClassicUrl(url));
    }    
}

