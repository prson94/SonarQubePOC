import { Input, Component, EventEmitter, Output, OnInit, OnChanges, SimpleChange, ChangeDetectionStrategy, ChangeDetectorRef  } from '@angular/core';
import { MetricsService } from '../../../services/metrics.service';
import { MetricAssetViewModel, MetricFieldTypeViewModel, ScoreType, ScoreTypeAllocation } from '../../../models/metrics.model';
import { TreeNode, MenuItem } from 'primeng/api';
import { BaseComponent } from '../../shared/base.component';
import { FormMode } from '../../../models/form.model';
import { AssetTypeMetricModel } from '../../../models/asset.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { AllocationService } from '../../../services/allocations.service';

@Component({
    selector: 'd3s-admin-metric-list',
    templateUrl: './admin-metric-list.component.html',
    providers: [MetricsService, AllocationService]
})

export class AdminMetricListComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() assetType: AssetTypeMetricModel;
    @Input() allocationUid: string;
    @Output() selectionChange = new EventEmitter();
    @Input() scoreType: ScoreTypeAllocation;
    @Input() scoreData: any;

    private metrics: MetricAssetViewModel[] = [];
    private metricTree: TreeNode[] = [];
    private selectedNode: TreeNode;
    private selection: MetricAssetViewModel;
    private previousSelection: MetricAssetViewModel;
    private previousSelectedNode: TreeNode;

    @Input() metricListFieldTypes: MetricFieldTypeViewModel[] = [];

    private formMode = FormMode.Default;
    FormMode = FormMode;

    private isExternallyCalculated: boolean = false;
    showDelete: boolean = false;

    private isHistoryModalVisible: boolean = false;

    constructor(private metricsService: MetricsService, private allocationService: AllocationService, protected messagesService: MessagesObservableService, ref: ChangeDetectorRef) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes['allocationUid'] && this.allocationUid) {
            this.formMode = FormMode.Default;
            this.load();
        }
        if (changes['scoreData'] && this.scoreData) {
            this.scoreData = [ ...this.scoreData ];
        }
    }

    load() {
        this.isLoading = true;
        this.metrics = [];
        this.metricTree = [];
        if (this.allocationUid) {
            this.metricsService.getMetricsByAllocation(this.allocationUid)
                .subscribe(r => {

                    this.metrics = r;
                    if (this.metrics) {
                        this.metrics.filter(g => g.ParentUid == null).forEach(g => {
                            let n = {
                                data: g,
                                children: [],
                                expanded: true
                            }
                            if (this.metricTree.findIndex(o => o.data.Uid === g.Uid) == -1) {
                                this.metricTree.push(n);
                                this.addChildren(n);
                            }
                        });
                    } else {
                        this.selectionChange.emit(null);
                    }

                    this.allocationService.getAllocationsByAssetTypeUid(this.assetType.Uid).subscribe(res => {
                        this.isLoading = false;
                        this.isExternallyCalculated = res.find(x => x.uid === this.allocationUid).isExternallyCalculated;
                        if (this.metricTree !== null && this.metricTree.length > 0) {
                            this.selection = this.metricTree[0].data;
                            this.selectionChange.emit(this.selection);
                            this.selectedNode = this.metricTree[0];
                        } else {
                            this.selectionChange.emit(null);
                        }
                    })
                });
        }
        else {
            this.selection = null;
            this.metricTree = [];
        }

    }

    addChildren(node: TreeNode) {
        let children = this.metrics.filter(g => g.ParentUid === node.data.Uid);
        if (children.length > 0) {
            children.forEach(c => {
                let n = {
                    data: c,
                    children: [],
                    expanded: true
                }
                node.children.push(n);
                this.addChildren(n);
            });
        }
    }

    public selectNode(e: any) {
        if (e == null)
            return;
        this.selectedNode = e;
        this.selection = e === null ? null : e.data;
        this.selectionChange.emit(this.selection);
    }

    public add(asChild: boolean = false) {
        if (this.selection)
            this.previousSelection = { ...this.selection };
        if (this.selectedNode)
        this.previousSelectedNode = { ...this.selectedNode };
        if (!asChild) {
            this.selection = null;
            this.selectedNode = null;
            this.selectionChange.emit(this.selection);
        }
        this.formMode = FormMode.Adding;
    }

    public edit() {
        this.previousSelection = { ...this.selection };
        this.previousSelectedNode = { ...this.selectedNode };
        this.formMode = FormMode.Editing;
    }

    public delete() {
        this.formMode = FormMode.Deleting;
    }    
    public close() {
        this.formMode = FormMode.Default;
        if (this.previousSelectedNode)
            this.selectedNode = { ...this.previousSelectedNode };
        if (this.previousSelection)
            this.selection = { ...this.previousSelection };
        this.selectionChange.emit(this.selection);
    }
    public showHistory(isHistoryVisible: boolean) {
        this.isHistoryModalVisible = isHistoryVisible;
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
    private getCardMenuItems(): MenuItem[] {
        var menu: MenuItem[] = [
            { label: 'Edit', command: (event) => { this.edit() } },
        ];
        menu.push({
            label: 'Disable',
            command: (event) => { this.delete(); }
        });
        menu.push({
            label: 'Version History (' + (this.selection ? this.selection.VersionCount : 0) + ')',
            command: (event) => { this.showHistory(true); }
        });
        return menu;
    }
};