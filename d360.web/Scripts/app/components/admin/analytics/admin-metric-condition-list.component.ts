import { Input, Component, EventEmitter, Output, OnInit, OnChanges } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { MetricsService } from '../../../services/metrics.service';
import { MetricFieldTypeViewModel, MetricAssetVersionConditionItemViewModel, MetricAssetVersionConditionItemFieldValueViewModel } from '../../../models/metrics.model';
import { FormMode } from '../../../models/form.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';

@Component({
    selector: 'd3s-admin-metric-condition-list',
    templateUrl: 'admin-metric-condition-list.component.html',
    providers: [MetricsService]
})

export class AdminMetricConditionListComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() conditionUid: string;
    @Input() position: number;
    @Input() assetTypeUid: string;
    @Input() conditions: MetricAssetVersionConditionItemViewModel[] = [];
    @Input() metricConditionListFieldTypes: MetricFieldTypeViewModel[] = [];

    @Output() editClick = new EventEmitter();
    @Output() deleteClick = new EventEmitter();
    @Output() addClick = new EventEmitter();
    @Output() conditionsChange = new EventEmitter();

    @Output() formModeChange = new EventEmitter();

    private usedFieldTypes: number[] = [];
    private selection: MetricAssetVersionConditionItemViewModel = null;
    private selectedIndex = -1;
    private formMode = FormMode.Default;
    FormMode = FormMode;

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

        this.conditions.forEach(c => {
            c.OperatorText = this.operators.find(o => o.value === c.Operator).label;

            const field = this.metricConditionListFieldTypes.find(f => f.ID === c.ConditionFieldTypeID);

            if (field !== null) {
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
        this.isLoading = false;

        return Promise.resolve();
    }

    add() {
        this.selection = new MetricAssetVersionConditionItemViewModel();
        this.selection.IsEditMode = false;
        this.formMode = FormMode.Adding;
        this.formModeChange.emit(this.formMode);
    }

    edit(e: any) {
        this.selection.IsEditMode = true;
        this.formMode = FormMode.Editing;
        this.formModeChange.emit(this.formMode);
    }

    delete(i: number) {
        this.selectedIndex = i;
        this.formMode = FormMode.Deleting;
        this.formModeChange.emit(this.formMode);
    }

    confirmDelete() {
        this.conditions.splice(this.selectedIndex, 1).slice();
        this.conditionsChange.emit(this.conditions);

        this.refreshSelectedFieldTypeIds();

        this.formMode = FormMode.Default;
        this.formModeChange.emit(this.formMode);
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
            this.usedFieldTypes.push(c.ConditionFieldTypeID);
        });
        console.log(this.usedFieldTypes);
    }
};