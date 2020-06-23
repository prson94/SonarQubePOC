import { Input, Component, EventEmitter, Output, OnInit } from '@angular/core';
import { MetricsService } from '../../../services/metrics.service';
import { MetricFieldTypeViewModel, MetricAssetVersionConditionItemViewModel } from '../../../models/metrics.model';
import { BaseComponent } from '../../shared/base.component';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { FormHelpers } from '../../../static/form-helpers';

@Component({
    selector: 'd3s-admin-metric-condition-editor',
    templateUrl: './admin-metric-condition-editor.component.html',
    providers: [MetricsService]
})

export class AdminMetricConditionEditorComponent extends BaseComponent implements OnInit {
    @Input() condition: MetricAssetVersionConditionItemViewModel = null;
    @Input() uid: string;
    @Input() metricConditionEditorFieldTypes: MetricFieldTypeViewModel[] = [];
    @Input() usedFieldTypes: number[] = [];
    @Input() assetTypeUid: string;
    @Output() onCancel = new EventEmitter();
    @Output() onSave = new EventEmitter();

    verb = "Add";

    constructor(private metricsService: MetricsService, protected messagesService: MessagesObservableService) {
        super();
    }

    ngOnInit() {

        //Set defaults;
        this.metricConditionEditorFieldTypes.forEach(ft => {
            ft.Disabled = false;
        });

        this.metricConditionEditorFieldTypes.sort((a, b) => a.Name.localeCompare(b.Name))

        this.usedFieldTypes.forEach(i => {
            const ft = this.metricConditionEditorFieldTypes.find(ft => ft.ID === i);
            if (ft) {
                if (this.condition) {
                    if (this.condition.ConditionFieldTypeID !== i) {
                        ft.Disabled = true;
                    }
                }
                else {
                    ft.Disabled = true;
                }
            }
        });

        this.load();
    }

    getLookupValues() {
        return this.metricConditionEditorFieldTypes.find(i => i.ID === +this.condition.ConditionFieldTypeID).Values;
    }

    load() {
        if (this.condition) {
            if (this.condition.ConditionFieldTypeID) {
                this.selectFieldType();
            }
        }
        this.isLoading = false;
    }

    valid() {
        let valid = true;

        if (this.condition === null) {
            valid = false;
        }

        return valid;
    }

    save() {

        if (this.condition.FieldType) {
            switch (this.condition.FieldType.Type) {
                case "Boolean":
                    this.condition.ValuesText = this.condition.SingleValue;
                    break;
                case "Lookup":
                    this.condition.ValuesText = this.condition.FieldType.Values.find(v => v.Value === +this.condition.SingleValue).Text; 
                    break;
                default:
                    this.condition.ValuesText = this.condition.SingleValue;
                    break;
            }
        }
        this.onSave.emit(this.condition);
    }

    cancel() {
        this.onCancel.emit();
    }

    changeFieldType(e: number) {
        this.condition.ConditionFieldTypeID = e;
        this.selectFieldType();
    }

    selectFieldType() {
        if (this.condition.ConditionFieldTypeID) {
            const field = this.metricConditionEditorFieldTypes.find(f => f.ID === +this.condition.ConditionFieldTypeID); 
            if (field) {
                this.condition.FieldTypeName = field.Name;
                this.condition.FieldType = field;
                if (!this.condition.Values) {
                    this.condition.Values = [];
                }

                if (this.condition.Values.length > 0) {
                    switch (field.Type) {
                        case "Boolean":
                            this.condition.SingleValue = (this.condition.Values[0].Value === 'true');
                            break;
                        case "Lookup":
                            this.condition.SingleValue = (this.condition.Values[0].Value);
                            break;
                        case "Date":
                        case "DateTime":
                            if (this.condition.Values) {
                                this.condition.SingleValue = new Date(this.condition.Values[0].Value as string);
                            }
                            break;
                        default:
                            this.condition.SingleValue = this.condition.Values[0].Value;
                            break;
                    }
                }
            }
        }
    }

    getLocaleDateString(): string {
        return FormHelpers.getLocaleDateString();
    }
};