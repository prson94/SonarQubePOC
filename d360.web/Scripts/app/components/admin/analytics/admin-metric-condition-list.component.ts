import { Input, Component, EventEmitter, Output, OnInit, OnChanges } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { MetricsService } from '../../../services/metrics.service';
import { MetricAssetVersionConditionViewModel, MetricFieldTypeViewModel, MetricFieldTypeValueViewModel } from '../../../models/metrics.model';
import { FormMode } from '../../../models/form.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';

@Component({
    selector: 'd3s-admin-metric-condition-list',
    templateUrl: 'admin-metric-condition-list.component.html',
    providers: [MetricsService]
})

export class AdminMetricConditionListComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() metricUid: string;
    @Input() assetTypeUid: string;
    @Input() conditions = [];
    @Input() metricConditionListFieldTypes: MetricFieldTypeViewModel[] = [];

    @Output() editClick = new EventEmitter();
    @Output() deleteClick = new EventEmitter();
    @Output() addClick = new EventEmitter();
    @Output() conditionsChange = new EventEmitter();

    @Output() formModeChange = new EventEmitter();

    private usedFieldTypeIDs: number[] = [];
    private selection: MetricAssetVersionConditionViewModel = null;
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
            c.OperatorText = this.operators.find(o => o.value == c.Operator).label;

            let field = this.metricConditionListFieldTypes.find(f => f.ID == c.FieldTypeID);
            if (field != null) {
                c.FieldTypeName = field.Name;
                c.Type = field.Type;
                if (field.Values) {
                    if (field.Values.length > 0) {
                        let valueModel: MetricFieldTypeValueViewModel = field.Values.find(o => o.Value == c.Values);
                        valueModel = field.Values.find(o => o.Value == c.Values);
                        if (valueModel) {
                            c.ValuesText = valueModel.Text;
                        }
                    }
                }

                if (!c.ValuesText) {
                    c.ValuesText = c.Values;
                }
            }
        });
        this.isLoading = false;

        return Promise.resolve();
    }

    add() {
        this.selection = new MetricAssetVersionConditionViewModel();
        this.selection.IsEditMode = false;
        //this.selection. = this.mapId;
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

    save(e: MetricAssetVersionConditionViewModel) {
        e.OperatorText = this.operators.find(o => o.value == e.Operator).label;

        if (!e.IsEditMode) {
            this.conditions.push(e);
        }

        this.refreshSelectedFieldTypeIds();

        this.conditions.slice();
        this.conditionsChange.emit(this.conditions);
        this.formMode = FormMode.Default;
        this.formModeChange.emit(this.formMode);
    }

    showAddButton() {
        return (this.usedFieldTypeIDs.length < this.metricConditionListFieldTypes.length);
    }

    refreshSelectedFieldTypeIds() {
        // Clear out the selected field type IDs, and reload.
        this.usedFieldTypeIDs = [];
        this.conditions.forEach(c => {
            this.usedFieldTypeIDs.push(c.FieldTypeID);
        });
    }
};