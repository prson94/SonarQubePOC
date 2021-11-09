import { Component, Input, OnInit, OnDestroy, EventEmitter, Output } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { MetricAssetHistoryViewModel, MetricAssetViewModel } from '../../../models/metrics.model';
import { MetricsService } from '../../../services/metrics.service';
import { TreeNode } from 'primeng/api';
import { AssetTypeMetricModel } from '../../../models/asset.model';
import { CommonScreenReferencesModel } from './common-screen-references-model';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'measure-history',
    templateUrl: `./measure-history.component.html`,
    providers: [MetricsService]
})

export class AdminMeasureHistoryComponent extends BaseComponent implements OnInit, OnDestroy {

    @Input() Measure: MetricAssetViewModel;
    @Input() AssetType: AssetTypeMetricModel;
    @Input() isExternallyCalculated: boolean = false;
    @Input() screenReferences: CommonScreenReferencesModel;
    @Output() onClose = new EventEmitter;

    private metricHistoryRecords: MetricAssetHistoryViewModel[] = [];
    private metricTree: TreeNode[] = [];
    private selectedNode: TreeNode;
    private selection: MetricAssetHistoryViewModel;
    private showConditions: boolean;

    showPassTest: boolean;

    constructor(
        private metricsService: MetricsService,
        protected settingsService: CompanySettingsService
    ) {
        super(settingsService);
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

  

    private hasConditions(item: MetricAssetHistoryViewModel) {
        if (item && item.ConditionGroups && item.ConditionGroups.length > 0) {
            return true;
        } else {
            return false;
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

        this.showConditions = this.hasConditions(this.selection);
        this.showPassTest = (this.hasPassTest(this.selection) && !this.Measure.IsGroup);
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