import { Component, OnInit, ChangeDetectionStrategy, HostListener, ChangeDetectorRef } from '@angular/core';
import { FieldsObservableService } from '../../services/fieldsObservable.service';
import { CompanySettingsService } from '../../services/settings.service';
import { OperatorModel } from '../../models/operator.model';
import { FieldTypeHelper } from '../../models/fieldtype-api.model';
import { FieldTypeAPIModelFieldCondition } from '../../models/field-condition-grid.models';
import { FormGroup, FormBuilder } from '@angular/forms';


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
    providers: [FieldsObservableService]
})

export class GalleryFieldConditionGridComponent implements OnInit {
    properties: Array<any>;
    sampleUsage: string = '<field-condition-grid [fields]="simpleExample" [conditions]="simpleValue" (onChange)="eventValue = $event"></field-condition-grid>';
    isLoading: boolean = true;

    assetTypeUid: string = '';
    fields: FieldTypeAPIModelFieldCondition[] = null;
    operators: OperatorModel[] = [];
    blank: any[] = [];
    formGroup: FormGroup;
    simpleValue: any;
    eventValue: any;    
    constructor(
        private fieldsService: FieldsObservableService,
        protected settingsService: CompanySettingsService,
        private fb: FormBuilder,
        private ref: ChangeDetectorRef
    ) {
        this.formGroup = fb.group({});
    }

    private cleanJsonExamples = {};

    ngOnInit(): void {
        this.properties = new Array();
        this.properties.push({ Name: "fields", Type: "Array<FieldTypeAPIModelFieldCondition>", Description: "Array of field items. Field values are limited to 150 characters or less.", Default: "Empty []" });
        this.properties.push({ Name: "conditions", Type: "Array<any>", Description: "Selection Value", Default: "Empty []" });
        this.properties.push({ Name: "onChange", Type: "Event Array<any>", Description: "Triggers on every change in grid condition form. Returns value.", Default: "initial value" });

        this.cleanJsonExamples['simpleExample'] = JSON.parse(JSON.stringify(this.simpleExample));
        this.cleanJsonExamples['preselectedExample'] = JSON.parse(JSON.stringify(this.preselectedExample));

    }

    loadData() {

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
                this.ref.markForCheck();
            });
        })

    }


    preselectedExample = [
        {
            "field": "StepNo",
            "operator": 9,
            "value": 12,
            "value2": 34
        },
        {
            "field": "GovernanceRole",
            "operator": 1,
            "value": "Value 1",
        },
        {
            "field": "Dateofservice",
            "operator": 8,
            "value": this.getFormattedDate(new Date())
        }
    ];


    simpleExample = [
        {
            "Name": "cstm_op",
            "FriendlyName": "Custom Operators Field",
            "Type": {
                "Text": {}
            },
            "Operators": [{
                "label": "zzz operator",
                "value": 2000
            }, {
                "label": "good operator",
                "value": 9
            }, {
                "label": "bad operator",
                "value": 12
            }, {
                "label": "no sorting here",
                "value": 15
            }
            ]
        },
        {
            "Name": "StepNo",
            "FriendlyName": "Step No",
            "Type": {
                "Decimal": {}
            },
            "Operators": [{
                "label": "is",
                "value": 1
            }, {
                "label": "is between",
                "value": 9
            }, {
                "label": "is greater than",
                "value": 12
            }, {
                "label": "is greater than or equal to",
                "value": 15
            }, {
                "label": "is less than",
                "value": 14
            }, {
                "label": "is less than or equal to",
                "value": 13
            }, {
                "label": "is not",
                "value": 2
            }, {
                "label": "is not populated",
                "value": 11
            }, {
                "label": "is populated",
                "value": 10
            }]
        }, {
            "Name": "Name",
            "FriendlyName": "Name",
            "Type": {
                "Text": {}
            },
            "Operators": [{
                "label": "contains",
                "value": 3
            }, {
                "label": "does not contain",
                "value": 4
            }, {
                "label": "ends with",
                "value": 6
            }, {
                "label": "is",
                "value": 1
            }, {
                "label": "is not",
                "value": 2
            }, {
                "label": "is not populated",
                "value": 11
            }, {
                "label": "is populated",
                "value": 10
            }, {
                "label": "starts with",
                "value": 5
            }]
        }, {
            "Name": "GovernanceRole",
            "FriendlyName": "Governance Role",
            "Type": {
                "Lookup": {}
            },
            "Operators": [{
                "label": "in",
                "value": 16
            }, {
                "label": "is",
                "value": 1
            }, {
                "label": "is not",
                "value": 2
            }, {
                "label": "is not populated",
                "value": 11
            }, {
                "label": "is populated",
                "value": 10
            }, {
                "label": "not in",
                "value": 17
            }],
            "Values": [{
                "value": "Value 1",
                "label": "Label 1"
            }, {
                "value": "Value 2",
                "label": "Label 2"
            }, {
                "value": "Value 3",
                "label": "Label 3"
            }, {
                "value": "Value 4",
                "label": "Label 4"
            }, {
                "value": "Value 5",
                "label": "Label 5"
            }, {
                "value": "Value 6",
                "label": "Label 6"
            }]
        },
        {
            "Name": "Dateofservice",
            "FriendlyName": "Date of service",
            "Type": {
                "Date": {}
            },
            "Operators": [{
                "label": "is",
                "value": 1
            }, {
                "label": "is after",
                "value": 8
            }, {
                "label": "is before",
                "value": 7
            }, {
                "label": "is between",
                "value": 9
            }, {
                "label": "is not",
                "value": 2
            }, {
                "label": "is not populated",
                "value": 11
            }, {
                "label": "is populated",
                "value": 10
            }]
        },
        {
            "Name": "Registrationtime",
            "FriendlyName": "Registration time",
            "Type": {
                "DateTime": {}
            },
            "Operators": [{
                "label": "is not populated",
                "value": 11
            }, {
                "label": "is populated",
                "value": 10
            }]
        }
    ];

    private getFormattedDate(date) {
        let year = date.getFullYear();
        let month = (1 + date.getMonth()).toString().padStart(2, '0');
        let day = date.getDate().toString().padStart(2, '0');

        return month + '/' + day + '/' + year;
    }
}
