import { Component, Input, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { Router } from '@angular/router';

import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { BaseComponent } from '../base.component';
import { GridDefinition, GridColumn, GridField, GridFilterColumn, GridFilterExpression, GridRelationshipFilterExpression } from '../../../models/grid-definition.model';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-dynamic-field-value-v2',
    templateUrl: './dynamic-field-value-v2.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class DynamicFieldValueComponentV2 extends BaseComponent implements OnInit {
    @Input() column: GridColumn;
    @Input() fields: GridField[] = [];
    @Input() item: any;
    @Input() isComplex: boolean = false;
    @Input() useApiName: boolean = false;
    @Input() isDateUTC: boolean = false;

    public fieldType: string;
    private fieldValue: any;
    private hasColor: boolean;
    private colorText: string;
    constructor(
        protected settingsService: CompanySettingsService,
        private router: Router) {
        super(settingsService);
    }

    ngOnInit() {
        this.fieldType = this.columnDataType(this.column);
        
        if (this.fieldType == 'date' && this.column.cellsformat && this.column.cellsformat == 'MM/dd/yyyy HH:mm:ss') {
            this.fieldType = 'datetime';
        }


        let colKey: string = null;
        if (this.useApiName && this.item && this.column && this.column.datafield) {
            var field = this.fields.filter(x => x.name.toLowerCase() == this.column.datafield.toLowerCase())[0];
            if (field && field.apiName) {
                colKey = field.apiName;
            }
            else {
                colKey = this.column.datafield;
            }

        }
        else if (this.item && this.column && this.column.datafield) {
            colKey = this.column.datafield;
        }

        if (colKey) {
            this.fieldValue = this.item[colKey];
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

        if (this.column['fieldType'] == 'Score' && this.fieldValue) {
            let thresholdKey = colKey + '_threshold';
            this.fieldValue = `<div class="score-pill-small score-${this.item[thresholdKey]}"></div><span>${this.fieldValue}</span>`;
        }

        if (this.fieldType == 'Color') {
            let hasValue = this.item[colKey] ? true : false;
            if (hasValue) {
                let parsedJSON = JSON.parse(this.item[colKey]);
                if (parsedJSON) {
                    this.hasColor = true;
                    this.fieldValue = parsedJSON.Value;
                    this.colorText = parsedJSON.Name;
                }
            } else {
                this.hasColor = false;
                this.colorText = 'None';
            }
        }
    }

    private formatAsNumber(): string {
        return this.fieldValue !== '' && this.fieldValue != null ? Number(this.fieldValue).toLocaleString() : "";
    }

    private formatAsPath(): string {
        let replacement = (this.fieldValue !== '' && this.fieldValue !== null ? this.fieldValue.split(" > ").join(" <i class='fa fa-angle-right'></i> ") : ""); 
        return replacement;
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
