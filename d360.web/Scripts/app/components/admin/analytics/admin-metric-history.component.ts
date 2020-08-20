import { Component, Input, OnInit, OnDestroy, EventEmitter, Output } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { MetricAssetVersionConditionItemViewModel, MetricAssetVersionConditionItemFieldValueViewModel, MetricFieldTypeViewModel, MetricAssetHistoryViewModel } from '../../../models/metrics.model';
import { MetricsService } from '../../../services/metrics.service';
import { TreeNode } from 'primeng/api';

@Component({
    selector: 'd3s-metric-history',
    templateUrl: `./admin-metric-history.component.html`,
    providers: [MetricsService]
})

export class AdminMetricHistoryComponent extends BaseComponent implements OnInit, OnDestroy {

    @Input() MeasureDisplayValue: string;
    @Input() AssetTypeDisplayValue: string;
    @Input() metricListFieldTypes: MetricFieldTypeViewModel[] = [];
    @Input() MeasureUid: string;
    @Input() isExternallyCalculated: boolean = false;
    @Output() onClose = new EventEmitter;

    private conditions: MetricAssetVersionConditionItemViewModel[] = [];
    private metricHistoryRecords: MetricAssetHistoryViewModel[] = [];
    private metricTree: TreeNode[] = [];
    private selectedNode: TreeNode;
    private selection: MetricAssetHistoryViewModel;
    private showConditions: boolean;

    private operators = [
        { value: 'eq', label: '=' },
        { value: 'neq', label: '!=' },
        { value: 'lt', label: '<' },
        { value: 'lte', label: '<=' },
        { value: 'gt', label: '>' },
        { value: 'gte', label: '>=' },
    ];

    constructor(
        private metricsService: MetricsService
    ) {
        super();
    }

    ngOnDestroy(): void {
        this.cancel();
    }

    ngOnInit() {
        this.isLoading = true;
        if (this.MeasureUid) {
            this.metricsService.getMetricsVersionHistory(this.MeasureUid)
                .subscribe(result => {
                    this.metricHistoryRecords = result;
                    if (this.metricHistoryRecords) {
                        this.metricHistoryRecords.forEach(g => {
                            let n = {
                                data: g,
                                children: [],
                                expanded: true
                            }

                            this.metricTree.push(n);

                        });
                        if (this.metricTree !== null && this.metricTree.length > 0) {
                            this.selectNode(this.metricTree[0]);
                        }
                    }
                    this.isLoading = false;
                });
        }
        else {
            this.selection = null;
            this.metricTree = [];
        }
    }

    cancel() {
        this.onClose.emit(null);
    }

    formatConditions() {
        this.conditions.forEach(c => {
            const field = this.metricListFieldTypes.find(f => f.ID === +c.ConditionFieldTypeID);
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

    private hasConditions(item: MetricAssetHistoryViewModel) {
        if (item && item.ConditionGroups && item.ConditionGroups.length > 0) {
            this.conditions = item.ConditionGroups[0].ConditionItems;
            this.formatConditions();
            return true;
        } else {
            this.conditions = [];
        }
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
            case 'Number':
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
    getAsPrecentage(val: number) {
        if (val == 0)
            return '0%';
        if (!val)
            return;
        if (val == 1)
            return '100%'
        let s = val + '0000';
        s = s.replace('0.', '');
        if (s.length > 6)
            s = (s.substr(0, 2)) + '.' + s[2] + "%";
        else
            s = (s.substr(0, 2)) + "%";
        if (s.startsWith('0'))
            s = s.substr(1, s.length);
        return s;
    }

    public selectNode(e: any) {
        if (e == null)
            return;
        this.selectedNode = e;
        this.selection = e === null ? null : e.data;

        if (this.hasConditions(this.selection)) {
            this.showConditions = true;
        }
        else {
            this.showConditions = false;
        }
    }
}