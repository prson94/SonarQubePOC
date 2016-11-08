import { Component, Input, Output, EventEmitter, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { BaseComponent } from './base.component';
import { GridDefinition, GridColumn, GridField, GridFilterColumn, GridFilterExpression, GridRelationshipFilterExpression, GridAttributeFilterExpression } from '../../models/grid-definition.model';

@Component({
    selector: 'd3s-dynamic-field-value',
    template: `   
            <span [ngSwitch]="fieldType">
                <span *ngSwitchCase="'date'">{{fieldValue | date:'shortDate'}}</span>
                <span *ngSwitchCase="'datetime'">{{fieldValue | date:'medium'}}</span>
                <span *ngSwitchCase="'number'">{{formatAsNumber()}}</span>                
                <span *ngSwitchCase="'bool'">
                    <i *ngIf="fieldValue == 'True'" class="fa fa-check enabled" title="True"></i>
                    <i *ngIf="fieldValue == 'False'" class="fa fa-times disabled" title="False"></i>
                </span>
                <span *ngSwitchDefault [innerHtml]="fieldValue"></span>                                        
            </span>
        `,
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class DynamicFieldValueComponent extends BaseComponent implements OnInit {       
    @Input() column: GridColumn;
    @Input() fields: GridField[] = [];
    @Input() item: any;

    private fieldType: string;
    private fieldValue: string;

    constructor() {
        super();
    }

    ngOnInit() {
        this.fieldType = this.columnDataType(this.column);

        if (this.fieldType == 'date' && this.column.cellsformat && this.column.cellsformat == 'MM/dd/yyyy HH:mm:ss') {
            this.fieldType = 'datetime';
        }
        
        if (this.item && this.column && this.column.datafield)
            this.fieldValue = this.item[this.column.datafield];
    }

    private formatAsNumber(): string {
        return this.fieldValue != '' && this.fieldValue != null ? Number(this.fieldValue).toLocaleString() : "";
    }

    private columnDataType(column: GridColumn): string {      
        var fields = this.fields.filter(x => x.name == column.datafield);

        if (fields.length > 0)
            return fields[0].type;
        return 'string';
    }
}