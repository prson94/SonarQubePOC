import { Input, Component, EventEmitter, Output, OnInit, OnChanges } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { MetricsService } from '../../../services/metrics.service';
import { MetricFieldTypeViewModel, MetricAssetVersionConditionItemViewModel, MetricAssetVersionConditionItemFieldValueViewModel, ScoreTypeAllocation } from '../../../models/metrics.model';
import { FormMode } from '../../../models/form.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';

@Component({
    selector: 'd3s-admin-metric-condition-list',
    templateUrl: 'admin-metric-condition-list.component.html',
    providers: [MetricsService]
})

export class AdminMetricConditionListComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() assetTypeUid: string;
    @Input() conditions: MetricAssetVersionConditionItemViewModel[] = [];
    @Input() metricConditionListFieldTypes: MetricFieldTypeViewModel[] = [];
   
    @Output() editClick = new EventEmitter();
    @Output() deleteClick = new EventEmitter();
    @Output() addClick = new EventEmitter();
    @Output() conditionsChange = new EventEmitter();

    @Output() formModeChange = new EventEmitter();

    private usedFieldTypes: string[] = [];
    private selection: MetricAssetVersionConditionItemViewModel = null;
    private selectedIndex = -1;
    private formMode = FormMode.Default;
    FormMode = FormMode;

    g: any = null;

    private operators = [
        { value: 'eq', label: '=' },
        { value: 'neq', label: '!=' },
        { value: 'lt', label: '<' },
        { value: 'lte', label: '<=' },
        { value: 'gt', label: '>' },
        { value: 'gte', label: '>=' },
    ];

    constructor(private metricsService: MetricsService, protected messagesService: MessagesObservableService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    ngOnChanges() {
        this.load();
    }

    load(): Promise<any> {
        this.isLoading = true;

        this.refreshSelectedFieldTypeIds();

        this.formatConditions();
        this.isLoading = false;

        return Promise.resolve();
    }
    formatConditions() {
        this.conditions.forEach(c => {
            const field = this.metricConditionListFieldTypes.find(f => f.ApiName === c.ConditionFieldTypeName);
            c.OperatorText = this.operators.find(o => o.value === c.Operator).label;
            c.OperatorText = this.parseOperator(field, c.OperatorText);

            if (field) {
                c.FieldTypeName = field.Name;
                c.FieldType = field;

                switch (field.Type) {
                    case 'Lookup':
                        if (field.Values) {
                            if (field.Values.length > 0) {
                                if (c.Values) {
                                    if (c.Values[0].Value) {
                                        let valueModel: MetricAssetVersionConditionItemFieldValueViewModel = field.Values.find(o => o.Value === +c.Values[0].Value);
                                        valueModel = field.Values.find(o => o.Value === +c.Values[0].Value);
                                        if (valueModel) {
                                            c.SingleValue = c.Values[0].Value;
                                            c.ValuesText = valueModel.Text;
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    default:
                        if (c.Values) {
                            if (c.Values[0].Value) {
                                c.SingleValue = c.Values[0].Value;
                                c.ValuesText = c.Values[0].Value;
                            }
                        }
                        break;
                }
            }
        });
    }

    parseOperator(field: MetricFieldTypeViewModel, OperatorText: string): string {
        switch (field.Type) {
            case 'Date':
                switch (OperatorText) {
                    case '=':
                        return 'is'
                    case '!=':
                        return 'is not'
                    case '<':
                        return 'is before'
                    case '>':
                        return 'is after'
                    case '<=':
                        return 'is on or before'
                    case '>=':
                        return 'is on or after'
                    default:
                        return OperatorText;
                }
            case 'Text':
            case 'Lookup':
                switch (OperatorText) {
                    case '=':
                        return 'is'
                    case '!=':
                        return 'is not'
                    default:
                        return OperatorText;
                }
            case 'Decimal':
            case'Number':
                switch (OperatorText) {
                    case '=':
                        return 'is'
                    case '!=':
                        return 'is not'
                    case '<':
                        return 'is before'
                    case '>':
                        return 'is after'
                    case '<=':
                        return 'is on or before'
                    case '>=':
                        return 'is on or after'
                    default:
                        return OperatorText;
                }
            case 'Boolean':
                switch (OperatorText) {
                    case '=':
                        return 'is'
                    default:
                        return OperatorText;
                }
        }
        return '';
    }


    save(e: MetricAssetVersionConditionItemViewModel) {
        e.OperatorText = this.operators.find(o => o.value === e.Operator).label;

        if (!e.IsEditMode) {
            this.conditions.push(e);
        }

        this.refreshSelectedFieldTypeIds();

        this.conditionsChange.emit(this.conditions);
        this.formMode = FormMode.Default;
        this.formModeChange.emit(this.formMode);
    }

    showAddButton() {
        return (this.usedFieldTypes.length < this.metricConditionListFieldTypes.length);
    }

    refreshSelectedFieldTypeIds() {
        // Clear out the selected field type IDs, and reload.
        this.usedFieldTypes = [];
        this.conditions.forEach(c => {
            this.usedFieldTypes.push(c.ConditionFieldTypeName);
        });
    }
};