import { Input, Component, EventEmitter, Output, OnInit, OnChanges } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { MetricsService } from '../../../services/metrics.service';
import { MetricFieldTypeViewModel, MetricAssetVersionConditionItemViewModel, MetricAssetVersionConditionItemFieldValueViewModel, ScoreTypeAllocation } from '../../../models/metrics.model';
import { FormMode } from '../../../models/form.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { OperatorModel } from '../../../models/operator.model';

@Component({
    selector: 'd3s-admin-metric-condition-list',
    templateUrl: 'admin-metric-condition-list.component.html',
    providers: [MetricsService]
})

export class AdminMetricConditionListComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() assetTypeUid: string;
    @Input() conditions: MetricAssetVersionConditionItemViewModel[] = [];
    @Input() metricConditionListFieldTypes: MetricFieldTypeViewModel[] = [];
    @Input() operators: OperatorModel[];
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
            c.OperatorText = this.operators.find(o => o.ID === c.Operator).Name;
 
            if (field) {
                c.FieldTypeName = field.Name;
                c.FieldType = field;

                switch (field.Type) {
                    case 'Lookup':
                        if (field.Values) {
                            if (field.Values.length > 0) {
                                if (c.Values) {
                                    if (c.Values[0]) {
                                        let valueModel: MetricAssetVersionConditionItemFieldValueViewModel = field.Values.find(o => o.Value === +c.Values[0]);
                                        valueModel = field.Values.find(o => o.Value === +c.Values[0]);
                                        if (valueModel) {
                                            c.SingleValue = c.Values[0];
                                            c.ValuesText = valueModel.Text;
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    default:
                        if (c.Values) {
                            if (c.Values[0]) {
                                c.SingleValue = c.Values[0];
                                c.ValuesText = c.Values[0];
                            }
                        }
                        break;
                }
            }
        });
    }

    save(e: MetricAssetVersionConditionItemViewModel) {
        e.OperatorText = this.operators.find(o => o.ID === e.Operator).Name;

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