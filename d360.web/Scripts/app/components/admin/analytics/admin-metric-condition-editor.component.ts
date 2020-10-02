import { Input, Component, EventEmitter, Output, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { MetricsService } from '../../../services/metrics.service';
import { MetricFieldTypeViewModel, MetricAssetVersionConditionItemViewModel, MetricAssetVersionConditionItemFieldValueViewModel } from '../../../models/metrics.model';
import { BaseComponent } from '../../shared/base.component';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { FormHelpers } from '../../../static/form-helpers';
import { SelectItem } from 'primeng/api';
import { OperatorModel, Operator } from '../../../models/operator.model';

@Component({
    selector: 'd3s-admin-metric-condition-editor',
    templateUrl: './admin-metric-condition-editor.component.html',
    providers: [MetricsService],
    changeDetection: ChangeDetectionStrategy.OnPush,
})

export class AdminMetricConditionEditorComponent extends BaseComponent implements OnInit {
    @Input() conditionItems: MetricAssetVersionConditionItemViewModel[] = [];
    @Input() uid: string;
    @Input() metricConditionEditorFieldTypes: MetricFieldTypeViewModel[] = [];
    @Input() assetTypeUid: string;
    @Output() onCancel = new EventEmitter();
    @Output() onSave = new EventEmitter();
    @Output() matchTypeChange = new EventEmitter();
    @Input() matchType: number;
    private fieldTypeDropdownOptions: SelectItem[] = []
    @Input() operators: OperatorModel[];
    private usedFieldTypes: string[] = [];

    private booleanOptions = [
        { value: "true", label: 'True' },
        { value: "false", label: 'False' }
    ];

    private newCondition: MetricAssetVersionConditionItemViewModel = new MetricAssetVersionConditionItemViewModel;

    verb = "Add";
    conditionsValid: boolean = true;

    constructor(private metricsService: MetricsService,
        protected messagesService: MessagesObservableService,
        private ref: ChangeDetectorRef) {
        super();
    }

    ngOnInit() {
        if (!this.conditionItems)
            this.conditionItems = [];

        this.metricConditionEditorFieldTypes.sort((a, b) => a.Name.localeCompare(b.Name))
        this.conditionsValid = true;

        this.checkSelectedFields();
        
        this.load();
    }

    load() {
        this.formatConditions();
        this.isLoading = false;
        this.ref.markForCheck();
    }

    valid() {
        let valid = true;

        if (this.newCondition === null) {
            valid = false;
        }
         return valid;
    }

    removeCondition(condition: MetricAssetVersionConditionItemViewModel) {
        const index = this.conditionItems.indexOf(condition);
        if (index > -1) {
            let item = this.conditionItems.splice(index, 1)[0];
            let ftIndex = this.usedFieldTypes.indexOf(item.ConditionFieldTypeName);

            if (ftIndex > -1) {
                this.usedFieldTypes.splice(ftIndex, 1);
                this.usedFieldTypes = [ ...this.usedFieldTypes ]; // Workaround so the angular filter pipe detects changes.
            }
        }
        this.checkSelectedFields();
        this.ref.markForCheck();
    }

    conditionFieldIsInvalid(condition: MetricAssetVersionConditionItemViewModel) {
        if (condition.ConditionFieldTypeName) {
            let other = this.conditionItems.filter(x => { return x.ConditionFieldTypeName == condition.ConditionFieldTypeName })
            return other.length > 1;
        }
        return false;
    }

    selectFieldType(condition: MetricAssetVersionConditionItemViewModel) {
        
        if (condition.ConditionFieldTypeName) {
            let field = this.metricConditionEditorFieldTypes.find(f => f.ApiName === condition.ConditionFieldTypeName); 
            if (field) {
                condition.FieldTypeName = field.Name;
                condition.FieldType = field;

                switch (field.Type) {
                    case "Boolean":
                        condition.SingleValue = null;
                        break;
                    case "Lookup":
                        condition.lookupOptions = this.metricConditionEditorFieldTypes.find(i => i.ApiName === condition.ConditionFieldTypeName).Values.map(x => { return { label: x.Text, value: x.Value } });
                        condition.SingleValue = null;
                        break;
                    case "Date":
                    case "DateTime":
                        condition.SingleValue = null;;
                        break;
                    default:
                        condition.SingleValue = null;
                        break;
                }
            }

            condition.operatorOptions = this.createOperatorOptions(field);

            //check for duplicate fieldTypeNames
            if (this.conditionItems.length > 1) {
                let fieldNames = this.conditionItems.map(x => { return x.ConditionFieldTypeName });
                this.conditionsValid = !fieldNames.some((item, inx) => { return fieldNames.indexOf(item) != inx });
            }
            this.checkSelectedFields();

            this.ref.markForCheck();
        }
    }

    checkSelectedFields() {
        // Set defaults.
        this.metricConditionEditorFieldTypes.forEach(ft => {
            ft.Disabled = false;
        });

        this.usedFieldTypes = (this.conditionItems) ? this.conditionItems.map(x => { return x.ConditionFieldTypeName }) : [];

        this.usedFieldTypes.forEach(i => {
            const ft = this.metricConditionEditorFieldTypes.find(ft => ft.ApiName === i);
            if (ft) {
                if (this.newCondition) {
                    if (this.newCondition.ConditionFieldTypeName !== i) {
                        ft.Disabled = true; 
                    }
                }
                else {
                    ft.Disabled = true;
                }
            }
        });

        this.fieldTypeDropdownOptions = this.metricConditionEditorFieldTypes.map((x) => { return { value: x.ApiName, label: x.Name, disabled: x.Disabled } });
    }

    getLocaleDateString(): string {
        return FormHelpers.getLocaleDateString();
    }

    formatConditions() {
        this.conditionItems.forEach(c => {
            const field = this.metricConditionEditorFieldTypes.find(f => f.ApiName === c.ConditionFieldTypeName);
            c.OperatorText = this.operators.find(o => o.ID === c.Operator).Name;

            if (field) {
                c.FieldTypeName = field.Name;
                c.FieldType = field;
                this.ref.markForCheck();
                switch (field.Type) {
                    case 'Lookup':
                        if (field.Values) {
                            if (field.Values.length > 0) {
                                c.lookupOptions = field.Values.map(x => { return { label: x.Text, value: x.Value } });
                                if (c.Values && c.Values.length > 0) {
                                    if (c.Values[0]) {
                                        let valueModel: MetricAssetVersionConditionItemFieldValueViewModel = field.Values.find(o => o.Value === +c.Values[0]);
                                        valueModel = field.Values.find(o => o.Value === +c.Values[0]);
                                        if (valueModel) {
                                            c.SingleValue = valueModel.Value;//c.Values[0];
                                            c.ValuesText = valueModel.Text;
                                        }
                                    }
                                }
                                this.ref.markForCheck();
                            }
                        }
                        break;
                    case 'Date':
                    case 'DateTime':
                        if (c.Values && c.Values.length > 0) {
                            if (c.Values[0]) {
                                let dt = new Date(c.Values[0]);
                                c.SingleValue = new Date(dt.getTime() + (dt.getTimezoneOffset() * 60000));
                                c.ValuesText = c.Values[0];
                            }
                        }
                        break;
                    default:
                        if (c.Values && c.Values.length > 0) {
                            if (c.Values[0]) {
                                c.SingleValue = c.Values[0];
                                c.ValuesText = c.Values[0];
                            }
                        }
                        break;
                }

                c.operatorOptions = this.createOperatorOptions(field);

                this.ref.markForCheck();
            }
        });
    }

    checkConditionOperator(condition: MetricAssetVersionConditionItemViewModel) {
        let operatorInfo = this.operators.find(o => { return o.ID == condition.Operator; });
        if (operatorInfo) {
            return (operatorInfo.MaximumValueCount > 0);
        }
        else {
            return true;
        }
    }

    createOperatorOptions(field: MetricFieldTypeViewModel) {
        let options = [];
        this.operators.forEach(op => {
            if (op.AllowedDataTypes.findIndex(dt => { return (dt.Name == field.Type); }) > -1) {
                options.push({ value: op.ID, label: op.Name });
            }
        });
        return options;
    }

    addNewCondition() {
        if (this.newCondition && this.newCondition.ConditionFieldTypeName) {
            this.newCondition.Operator = Operator.Equals;
            this.selectFieldType(this.newCondition);
            this.conditionItems.push({ ...this.newCondition });
            this.newCondition = new MetricAssetVersionConditionItemViewModel();

            this.checkSelectedFields();
            this.formatConditions();

            this.ref.markForCheck();
        }
    }

    matchTypeChangeEvt() {
        this.matchTypeChange.emit(this.matchType);
    }
    
    doToggle(evt: MouseEvent, pc: any) {
        let htmlEl = evt.target as Element;
        if (htmlEl.classList.contains('ui-inputtext')) {
            evt.stopPropagation();
            return;
        }
        pc.toggle();
    }
};