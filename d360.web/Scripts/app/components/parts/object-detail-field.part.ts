///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, OnInit, Output, EventEmitter } from '@angular/core';
import { DetailRow, DetailField, DetailModel, DetailFieldType } from '../../models/object-detail.model';

@Component({
    selector: 'object-detail-field',
    template: `
            <div *ngIf="field.Type == DetailFieldType.Field && field.Name == 'Email'" class="FieldDisplayContent"><a [href]="'mailto:' + field.Value">{{field.Value}}</a></div>
            <div *ngIf="field.Type == DetailFieldType.Field && field.Name != 'Email'" class="FieldDisplayContent" [innerHtml]="field.Value"></div>
            <div *ngIf="field.Type == DetailFieldType.Tooltip" class="FieldDisplayContent">
                <d3s-tooltip [tooltipType]="field.TooltipContext" [objectType]="field.TooltipType" [objectId]="field.TooltipID">
                    <a [href]="field.TooltipUrl" [innerHtml]="field.Value"></a>
                </d3s-tooltip>

            </div>
            <div *ngIf="field.Type == DetailFieldType.Lookup">
                <d3s-dynamic-lookup-grid *ngIf="field.Data && field.Data.Values && field.Data.Values.length > 0" [data]="field.Data"></d3s-dynamic-lookup-grid>
            </div>
    `
})

export class ObjectDetailField implements OnInit {
    @Input() field: DetailField;
    DetailFieldType = DetailFieldType;

    constructor() {
    }

    ngOnInit() {

    }
}

