import { Component, OnInit, ChangeDetectionStrategy, HostListener } from '@angular/core';
import { FieldsObservableService } from '../../services/fieldsObservable.service';
import { FieldTypeAPIModelFieldCondition } from '../shared/controls/field-condition-grid/field-condition-grid.models';
import { CompanySettingsService } from '../../services/settings.service';
import { CurrentCompanySettings } from '../../static/company-settings';
import { OperatorModel } from '../../models/operator.model';
import { FieldTypeHelper } from '../../models/fieldtype-api.model';


@Component({
    selector: 'gallery-field-condition-grid',
    templateUrl: './gallery.field-condition-grid.component.html',
    styles: [
        `
        .gallery-section {
            padding: 0 16px 32px 16px;
        }

        .gallery-section h4 {
            padding-bottom: 8px;
        }

        `
    ],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [FieldsObservableService, CompanySettingsService]
})

export class GalleryFieldConditionGridComponent implements OnInit {
    protected properties: Array<any>;
    protected sampleUsage: string = '<ig-popup-menu [items]="items"></ig-popup-menu>';
    protected isLoading1: boolean = true;
    protected isLoading2: boolean = false;

    assetTypeUid: string = '2dc15e42-b2fc-4eb4-bc0d-40850c54b9aa';
    fields: FieldTypeAPIModelFieldCondition[] = null;
    operators: OperatorModel[] = [];

    constructor(
        private fieldsService: FieldsObservableService,
        private settingsService: CompanySettingsService
    ) {

    }


    ngOnInit(): void {
        this.properties = new Array();
        this.properties.push({ Name: "items", Type: "Array<PopupMenuItem>", Description: "Array of menu items", Default: "Empty []" });

        this.settingsService.getOperators().subscribe(operators => {
            this.operators = operators;
            this.fieldsService.getFieldsV2(this.assetTypeUid, null, null).subscribe(res => {
                var tempFields = [];
                res.forEach(f => {
                    if (FieldTypeHelper.isFieldForOperator(f.Type)) {
                        tempFields.push(f as FieldTypeAPIModelFieldCondition);
                    }
                });

                tempFields.forEach(f => {
                    f.Operators = [];
                    this.operators.forEach(op => {
                        if (op.AllowedDataTypes.some(x => x.Name === FieldTypeHelper.getFieldType(f.Type))) {
                            f.Operators.push({ label: op.Name, value: op.ID });
                        }

                        if (FieldTypeHelper.getFieldType(f.Type) === 'Lookup') {
                            f.Values = [];
                            f.Values.push({ value: 'Value 1', label: 'Label 1' });
                            f.Values.push({ value: 'Value 2', label: 'Label 2' });
                            f.Values.push({ value: 'Value 3', label: 'Label 3' });
                            f.Values.push({ value: 'Value 4', label: 'Label 4' });
                            f.Values.push({ value: 'Value 5', label: 'Label 5' });
                            f.Values.push({ value: 'Value 6', label: 'Label 6' });
                        }

                        if (FieldTypeHelper.getFieldType(f.Type) === 'Boolean') {
                            f.Values = [];
                            f.Values.push({ value: 'true', label: 'True' });
                            f.Values.push({ value: 'false', label: 'False' });
                        }
                    });

                });

                this.fields = tempFields;
                this.selectedValue = JSON.parse(JSON.stringify(this.selectedValueTemp));
            });
        })

    }

    private selectedValue = null;

    private selectedValueTemp = [
        {
            "field": "Booleanvalue",
            "operator": 11,
            "value": ""
        },
        {
            "field": "Dateofservice",
            "operator": 8,
            "value": this.getFormattedDate(new Date())
        },
        {
            "field": "Booleanvalue",
            "operator": 1,
            "value": "true"
        },
        {
            "field": "Name",
            "operator": 2,
            "value": "test"
        },
        {
            "field": "Countrypicker",
            "operator": 17,
            "value": "Value 5"
        },
        {
            "field": "StepNo",
            "operator": 2,
            "value": 2
        },
        {
            "field": "",
            "operator": "",
            "value": null
        }
    ]

    private getFormattedDate(date) {
        let year = date.getFullYear();
        let month = (1 + date.getMonth()).toString().padStart(2, '0');
        let day = date.getDate().toString().padStart(2, '0');

        return month + '/' + day + '/' + year;
    }
}
