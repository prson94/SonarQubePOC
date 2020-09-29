import { Component, Input, OnInit, OnDestroy, EventEmitter, Output } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { MetricAssetVersionConditionItemViewModel, MetricAssetVersionConditionItemFieldValueViewModel, MetricFieldTypeViewModel, MetricAssetHistoryViewModel } from '../../../models/metrics.model';
import { MetricsService } from '../../../services/metrics.service';
import { TreeNode } from 'primeng/api';
import { OperatorModel } from '../../../models/operator.model';

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
    @Input() operators: OperatorModel[];

    private conditions: MetricAssetVersionConditionItemViewModel[] = [];
    private metricHistoryRecords: MetricAssetHistoryViewModel[] = [];
    private metricTree: TreeNode[] = [];
    private selectedNode: TreeNode;
    private selection: MetricAssetHistoryViewModel;
    private showConditions: boolean;

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
            const field = this.metricListFieldTypes.find(f => f.ApiName === c.ConditionFieldTypeName);
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

    private hasConditions(item: MetricAssetHistoryViewModel) {
        if (item && item.ConditionGroups && item.ConditionGroups.length > 0) {
            this.conditions = item.ConditionGroups[0].ConditionItems;
            this.formatConditions();
            return true;
        } else {
            this.conditions = [];
        }
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