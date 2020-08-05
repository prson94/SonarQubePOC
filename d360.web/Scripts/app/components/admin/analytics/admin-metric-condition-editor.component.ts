import { Input, Component, EventEmitter, Output, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { MetricsService } from '../../../services/metrics.service';
import { MetricFieldTypeViewModel, MetricAssetVersionConditionItemViewModel, MetricAssetVersionConditionItemFieldValueViewModel } from '../../../models/metrics.model';
import { BaseComponent } from '../../shared/base.component';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { FormHelpers } from '../../../static/form-helpers';
import { SelectItem } from 'primeng/api';
import { forEach } from 'core-js/fn/dict';

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
    private usedFieldTypes: number[] = [];

    private booleanOptions = [
        { value: "true", label: 'True' },
        { value: "false", label: 'False' }
    ];
    private operators = [
        { value: 'eq', label: '=' },
        { value: 'neq', label: '!=' },
        { value: 'lt', label: '<' },
        { value: 'lte', label: '<=' },
        { value: 'gt', label: '>' },
        { value: 'gte', label: '>=' },
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
        //Set defaults;
        this.metricConditionEditorFieldTypes.forEach(ft => {
            ft.Disabled = false;
        });
        this.usedFieldTypes = this.conditionItems.map(x => { return x.ConditionFieldTypeID });
        this.metricConditionEditorFieldTypes.sort((a, b) => a.Name.localeCompare(b.Name))
        this.conditionsValid = true;
        this.usedFieldTypes.forEach(i => {
            const ft = this.metricConditionEditorFieldTypes.find(ft => ft.ID === +i);
            if (ft) {
                if (this.newCondition) {
                    if (this.newCondition.ConditionFieldTypeID !== +i) {
                        ft.Disabled = true; 
                    }
                }
                else {
                    ft.Disabled = true;
                }
            }
        });

        this.fieldTypeDropdownOptions = this.metricConditionEditorFieldTypes.map((x) => { return { value: x.ID, label: x.Name, disabled: x.Disabled } });
        this.load();
    }

    getLookupValues(condition: MetricAssetVersionConditionItemViewModel) {
        return this.metricConditionEditorFieldTypes.find(i => i.ID === +condition.ConditionFieldTypeID).Values.map(x => { return { label: x.Text, value: x.Value } });
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
            let ftIndex = this.usedFieldTypes.indexOf(item.ConditionFieldTypeID);

            if (ftIndex > -1) {
                this.usedFieldTypes.splice(ftIndex, 1);
                this.usedFieldTypes = [ ...this.usedFieldTypes ];//workaroud so the angular filter pipe detects changes
            }
        }
        this.ref.markForCheck();
    }

    conditionFieldIsInvalid(condition: MetricAssetVersionConditionItemViewModel) {
        if (condition.ConditionFieldTypeID) {
            let other = this.conditionItems.filter(x => { return x.ConditionFieldTypeID == condition.ConditionFieldTypeID })
            return other.length > 1;
        }
        return false;
    }

    selectFieldType(condition: MetricAssetVersionConditionItemViewModel) {
       
        if (condition.ConditionFieldTypeID) {
            let field = this.metricConditionEditorFieldTypes.find(f => f.ID === +condition.ConditionFieldTypeID); 
            if (field) {
                condition.FieldTypeName = field.Name;
                condition.FieldType = field;
                if (!condition.Values) {
                    condition.Values = [];
                }

                if (condition.Values.length > 0) {
                    switch (field.Type) {
                        case "Boolean":
                            condition.SingleValue = (condition.Values[0].Value === 'true');
                            break;
                        case "Lookup":
                            condition.SingleValue = (condition.Values[0].Value);
                            break;
                        case "Date":
                        case "DateTime":
                            if (condition.Values) {
                                condition.SingleValue = new Date(condition.Values[0].Value as string);
                            }
                            break;
                        default:
                            condition.SingleValue = condition.Values[0].Value;
                            break;
                    }
                }                
            }
            let options = [];
            switch (field.Type) {
                case 'Text':
                case 'Lookup':
                    options = [{ value: 'neq', label: '!=' }, { value: 'eq', label: '=' }];
                    break;
                case 'Decimal':
                case 'Number':
                case 'Date':
                    options = [
                        { value: 'eq', label: '=' },
                        { value: 'neq', label: '!=' },
                        { value: 'lt', label: '<' },
                        { value: 'lte', label: '<=' },
                        { value: 'gt', label: '>' },
                        { value: 'gte', label: '>=' },
                    ];
                    break;
                case 'Boolean':
                    options = [{ value: 'eq', label: '=' }];
                    break;
            }
            condition.operatorOptions = options;

            //check duplicate fieldTypeIDs
            if (this.conditionItems.length > 1) {
                let fieldIds = this.conditionItems.map(x => { return x.ConditionFieldTypeID });
                this.conditionsValid = !fieldIds.some((item, inx) => { return fieldIds.indexOf(item) != inx });
            }

            this.ref.markForCheck();
        }
    }

    getLocaleDateString(): string {
        return FormHelpers.getLocaleDateString();
    }

    formatConditions() {
        this.conditionItems.forEach(c => {
            const field = this.metricConditionEditorFieldTypes.find(f => f.ID === +c.ConditionFieldTypeID);
            c.OperatorText = this.operators.find(o => o.value === c.Operator).label;
            c.OperatorText = this.parseOperator(c, c.OperatorText);

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
                                            c.SingleValue = +c.Values[0].Value;
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
                let options = [];
                switch (field.Type) {
                    case 'Text':
                    case 'Lookup':
                        options = [{ value: 'neq', label: '!=' }, { value: 'eq', label: '=' }];
                        break;
                    case 'Decimal':
                    case 'Number':
                    case 'Date':
                        options = [
                            { value: 'eq', label: '=' },
                            { value: 'neq', label: '!=' },
                            { value: 'lt', label: '<' },
                            { value: 'lte', label: '<=' },
                            { value: 'gt', label: '>' },
                            { value: 'gte', label: '>=' },
                        ];
                        break;
                    case 'Boolean':
                        options = [{ value: 'eq', label: '=' }];
                        break;
                }
                c.operatorOptions = options;

                this.ref.markForCheck();
            }
        });
    }

    parseOperator(condition: MetricAssetVersionConditionItemViewModel, OperatorText: string): string {
        let field = this.metricConditionEditorFieldTypes.find(ft => ft.ID === condition.ConditionFieldTypeID);
        if (field) {
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
                case 'Number':
                    switch (OperatorText) {
                        case '=':
                            return 'is'
                        case '!=':
                            return 'is not'
                        case '<':
                            return 'is less than'
                        case '>':
                            return 'is greater than'
                        case '<=':
                            return 'is no greater than'
                        case '>=':
                            return 'is no less than'
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
        }
        return OperatorText;
    }

    addNewCondition() {
        if (this.newCondition && this.newCondition.ConditionFieldTypeID) {
            this.newCondition.Operator = "eq";
            this.selectFieldType(this.newCondition);
            this.conditionItems.push({ ...this.newCondition });
            this.usedFieldTypes = [...this.usedFieldTypes, this.newCondition.ConditionFieldTypeID];
            
            this.newCondition = new MetricAssetVersionConditionItemViewModel();
            
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