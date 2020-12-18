import { Component, Input, OnInit, OnDestroy, EventEmitter, Output } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { MetricAssetVersionConditionItemViewModel, MetricAssetVersionConditionItemFieldValueViewModel, MetricFieldTypeViewModel, MetricAssetHistoryViewModel, MetricAssetDefinitionGovernanceViewModel, MetricAssetViewModel, MetricPathOptionViewModel } from '../../../models/metrics.model';
import { MetricsService } from '../../../services/metrics.service';
import { TreeNode } from 'primeng/api';
import { OperatorModel, Operator } from '../../../models/operator.model';
import { AssetType, AssetTypeMetricModel } from '../../../models/asset.model';

@Component({
    selector: 'measure-history',
    templateUrl: `./measure-history.component.html`,
    providers: [MetricsService]
})

export class AdminMeasureHistoryComponent extends BaseComponent implements OnInit, OnDestroy {

    @Input() Measure: MetricAssetViewModel;
    @Input() AssetType: AssetTypeMetricModel;
    @Input() assetTypeFields: MetricFieldTypeViewModel[] = [];
    @Input() isExternallyCalculated: boolean = false;
    @Input() operators: OperatorModel[];    
    @Input() paths: MetricPathOptionViewModel[] = [];
    @Input() responsibilityTypes: any[] = [];
    @Input() relationshipTypes: any[] = [];

    @Output() onClose = new EventEmitter;
    
    private conditions: MetricAssetVersionConditionItemViewModel[] = [];
    private metricHistoryRecords: MetricAssetHistoryViewModel[] = [];
    private metricTree: TreeNode[] = [];
    private selectedNode: TreeNode;
    private selection: MetricAssetHistoryViewModel;
    private showConditions: boolean;

    showPassTest: boolean;

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
        if (this.Measure.Uid) {
            this.metricsService.getMetricsVersionHistory(this.Measure.Uid)
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
            const field = this.assetTypeFields.find(f => f.ApiName === c.ConditionFieldTypeName);
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
                                        let valueModel: MetricAssetVersionConditionItemFieldValueViewModel = field.Values.find(o => o.Value === c.Values[0]);
                                        valueModel = field.Values.find(o => o.Value === c.Values[0]);
                                        if (valueModel) {
                                            c.SingleValue = c.Values[0];
                                            c.ValuesText = valueModel.Text;
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case 'Date':
                        if (c.Values) {
                            if (c.Values[0]) {
                                c.SingleValue = c.Values[0];
                                c.ValuesText = new Date(c.Values[0]).toLocaleDateString();
                            }
                        }
                        break;
                    case 'DateTime':
                        if (c.Values) {
                            if (c.Values[0]) {
                                c.SingleValue = c.Values[0];
                                c.ValuesText = new Date(c.Values[0]).toLocaleString();
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
     
        if (this.hasPassTest(this.selection) && !this.Measure.IsGroup)
            this.showPassTest = true
        else
            this.showPassTest = false;
    }

    private hasPassTest(item: MetricAssetHistoryViewModel) {
        if (item &&
            item.Definition &&
            (item.Definition.DataQuality || (item.Definition.Governance && item.Definition.Governance.Check))
        ) {
            return true;
        } else {
            return false;
        }
    }
}