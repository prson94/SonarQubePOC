import { Component, Input, Output, EventEmitter, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { Router } from '@angular/router';

import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { BaseComponent } from '../base.component';
import { GridDefinition, GridColumn, GridField, GridFilterColumn, GridFilterExpression, GridRelationshipFilterExpression } from '../../../models/grid-definition.model';

@Component({
    selector: 'd3s-dynamic-field-value',
    templateUrl: './dynamic-field-value.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class DynamicFieldValueComponent extends BaseComponent implements OnInit {
    @Input() column: GridColumn;
    @Input() fields: GridField[] = [];
    @Input() item: any;
    @Input() isComplex: boolean = false;
    @Input() useApiName: boolean = false;
    @Input() isDateUTC: boolean = false;

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
        if (this.useApiName && this.item && this.column && this.column.datafield) {
            var field = this.fields.filter(x => x.name.toLowerCase() == this.column.datafield.toLowerCase())[0];
            if (field && field.apiName) {
                this.fieldValue = this.item[field.apiName];
            }
            else {
                this.fieldValue = this.item[this.column.datafield];
            }

        }
        else if (this.item && this.column && this.column.datafield) {
            this.fieldValue = this.item[this.column.datafield];
        }

        if ((this.fieldType == 'bool') && (typeof this.fieldValue === 'boolean')) {
            this.fieldValue = this.fieldValue ? "True" : "False"; // fix for bools as bools.        
        }

        if (this.fieldType == 'bool' && this.fieldValue) {
            this.fieldValue = this.fieldValue.toUpperCase(); //fix for miXeD CaSe booleans!
        }

        if ((this.fieldType == 'date' || this.fieldType == 'datetime') && isNaN(Date.parse(this.fieldValue)))
            this.fieldValue = null;

        if (this.useApiName && this.column['fieldType'] == 'Link' && this.fieldValue) {
            var delimiterIdx = (this.fieldValue as string).indexOf('|');
            if (delimiterIdx > -1) {
                var name = (this.fieldValue as string).substring(0, delimiterIdx);
                var href = (this.fieldValue as string).substring(delimiterIdx + 1);
                this.fieldValue = `<a href="${href}" target="_blank">${name}</a>`;
            }
        }

    }

    private formatAsNumber(): string {
        return this.fieldValue !== '' && this.fieldValue != null ? Number(this.fieldValue).toLocaleString() : "";
    }

    private columnDataType(column: GridColumn): string {
        var fields = this.fields.filter(x => x.name == column.datafield);

        if (column.type == 'preview')
            return 'preview';
        if ((column.datafield == 'Name' || column.datafield == 'TextPath') && !this.isComplex) {
            if (column['objectfield'] != null && column['objectidfield'] != null)
                return 'preview';
            else
                return 'string';
        }


        if (fields.length > 0)
            return fields[0].type;
        return 'string';
    }

    private navigate(url: string) {
        this.router.navigateByUrl(SiteUrlHelpers.convertClassicUrl(url));
    }
}
