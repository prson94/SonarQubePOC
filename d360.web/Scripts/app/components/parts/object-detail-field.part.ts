///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, OnInit, Output, EventEmitter } from '@angular/core';
import { DetailRow, DetailField, DetailModel, DetailFieldType } from '../../models/object-detail.model';

@Component({
    selector: 'object-detail-field',
    template: `
            <div *ngIf="field.Type == DetailFieldType.Field" class="FieldDisplayContent" [innerHtml]="field.Value"></div>
            <div *ngIf="field.Type == DetailFieldType.Tooltip" class="FieldDisplayContent">
                <a [href]="field.TooltipUrl" [innerHtml]="field.Value"></a>
            </div>
            <div *ngIf="field.Type == DetailFieldType.Lookup">
                <simple-accordion>
                    {{field.LookupGridUrl}}
                </simple-accordion>
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

