import { Component, Input, Output, EventEmitter, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { Router } from '@angular/router';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { BaseComponent } from '../base.component';
import { GridDefinition, GridColumn, GridField, GridFilterColumn, GridFilterExpression, GridRelationshipFilterExpression, GridAttributeFilterExpression } from '../../../models/grid-definition.model';

@Component({
    selector: 'd3s-dynamic-field-value',
    template: `   
    <style>
    a,
    span {
        word-break: break-all;
    }
</style>
            <span [ngSwitch]="fieldType">
                <span *ngSwitchCase="'date'" ><span *ngIf="fieldValue">{{fieldValue | date:'shortDate'}}</span></span>
                <span *ngSwitchCase="'datetime'"><span *ngIf="fieldValue">{{fieldValue | date:'medium'}}</span></span>
                <span *ngSwitchCase="'number'">{{formatAsNumber()}}</span>                
                <span *ngSwitchCase="'bool'">
                    <i *ngIf="fieldValue == 'TRUE'" class="fa fa-check enabled" title="True"></i>
                    <i *ngIf="fieldValue == 'FALSE'" class="fa fa-times disabled" title="False"></i>
                </span>
                <span *ngSwitchCase="'lookup'">
                    <d3s-lookup-tooltip [objectType]="item[column.objectfield]" [objectId]="item[column.objectidfield]">
                        <a (click)="navigate(item[column.urlfield])" [innerText]="fieldValue"></a> 
                    </d3s-lookup-tooltip>                    
                </span>
                <span *ngSwitchCase="'string'">{{fieldValue}}</span>                                                        
                <ng-template ngSwitchDefault>
                    <span *ngIf="fieldValue != null" [innerHtml]="fieldValue"></span>                                        
                </ng-template>
            </span>
        `,
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class DynamicFieldValueComponent extends BaseComponent implements OnInit {       
    @Input() column: GridColumn;
    @Input() fields: GridField[] = [];
    @Input() item: any;
    @Input() isComplex: boolean = false;

    public fieldType: string;
    private fieldValue: any;

    constructor(private router: Router) {
        super();
    }

    ngOnInit() {
        this.fieldType = this.columnDataType(this.column);

        if (this.fieldType == 'date' && this.column.cellsformat && this.column.cellsformat == 'MM/dd/yyyy HH:mm:ss') {
            this.fieldType = 'datetime';
        }

        if (this.item && this.column && this.column.datafield)
            this.fieldValue = this.item[this.column.datafield];

        if ((this.fieldType == 'bool') && (typeof this.fieldValue === 'boolean')) {
            this.fieldValue = this.fieldValue ? "True" : "False"; // fix for bools as bools.        
        }

        if (this.fieldType == 'bool' && this.fieldValue) {
            this.fieldValue = this.fieldValue.toUpperCase(); //fix for miXeD CaSe booleans!
        }   

        if ((this.fieldType == 'date' || this.fieldType == 'datetime') && isNaN(Date.parse(this.fieldValue)))
            this.fieldValue = null;
    }

    private formatAsNumber(): string {        
        return this.fieldValue !== '' && this.fieldValue != null ? Number(this.fieldValue).toLocaleString() : "";
    }

    private columnDataType(column: GridColumn): string {      
        var fields = this.fields.filter(x => x.name == column.datafield);

        if (column.type == 'preview')
            return 'preview';
        if ((column.datafield == 'Name' || column.datafield == 'TextPath') && !this.isComplex)
            return 'tooltip';

        if (fields.length > 0)
            return fields[0].type;
        return 'string';
    }

    private navigate(url: string) {
        this.router.navigateByUrl(SiteUrlHelpers.convertClassicUrl(url));
    }
}

