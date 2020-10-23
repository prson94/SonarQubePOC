import { Input, Component, EventEmitter, Output, OnInit, OnChanges, SimpleChange } from '@angular/core';
import { MetricsService } from '../../../services/metrics.service';
import { MetricAssetViewModel, MetricFieldTypeViewModel, ScoreTypeAllocation } from '../../../models/metrics.model';
import { TreeNode, MenuItem } from 'primeng/api';
import { BaseComponent } from '../../shared/base.component';
import { FormMode } from '../../../models/form.model';
import { AssetTypeMetricModel } from '../../../models/asset.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { AllocationService } from '../../../services/allocations.service';
import { OperatorModel } from '../../../models/operator.model';

@Component({
    selector: 'd3s-admin-metric-list',
    templateUrl: './admin-metric-list.component.html',
    providers: [MetricsService, AllocationService],
    styles: [
        `
        .ig-badge.default {
            border: 1px solid rgba(0,0,0,0.2); 
        }
        p-checkbox{
            margin-right: 32px; 
        }
        .badge-container{
            margin-left: 16px;
        }
        `
    ],
    encapsulation: ViewEncapsulation.None
})

export class AdminMetricListComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() assetType: AssetTypeMetricModel;
    @Input() allocationUid: string;
    @Output() selectionChange = new EventEmitter();
    @Input() scoreType: ScoreTypeAllocation;
    @Input() scoreData: any;
    @Input() operators: OperatorModel[];
    @Input() showDisabled: boolean = false;

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

    menuClicked($event) {
        switch ($event.value) {
            case 'Edit': this.edit();
                break;
            case 'Disable': this.delete();
                break;
        }

        if ($event.value.toString().indexOf('Version History') != -1)
            this.showHistory(true);
    }

    private menuOptions = [
        {
            "title": "Edit"
        },
        {
            "title": "Disable"
        },
        {
            "title": "Version History"
        }
    ];


    constructor(private metricsService: MetricsService, private allocationService: AllocationService, protected messagesService: MessagesObservableService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        let requiresLoad = false;
        if (changes['allocationUid'] && this.allocationUid) {
            requiresLoad = true;
        }
        if (changes['scoreData'] && this.scoreData) {
            this.scoreData = [...this.scoreData];
        }
        console.log(changes['showDisabled']);
        if (changes['showDisabled'] != null || changes['showDisabled'] != undefined) {
            requiresLoad = true;
        }

        if (requiresLoad) {
            this.formMode = FormMode.Default;
            this.load();
        }
    }

    load(initiallySelected?: string) {
        this.isLoading = true;
        this.metrics = [];
        this.metricTree = [];
        if (this.allocationUid) {
            console.log(this.showDisabled);
            this.metricsService.getMetricsByAllocation(this.allocationUid, this.showDisabled)
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
                            let node = this.metricTree[0];
                            if (initiallySelected) {
                                let found = null;
                                this.metricTree.forEach(n => {
                                    if (n.data.Name.toLowerCase() === initiallySelected.toLowerCase())
                                        found = n;
                                    else if (n.children && n.children.length > 0) {
                                        found = n.children.find(c => c.data.Name.toLowerCase() === initiallySelected.toLowerCase())
                                    }


                                });
                                if (found)
                                    node = found;

                            }
                            this.selection = node.data;
                            this.selectionChange.emit(this.selection);
                            this.selectedNode = node;
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
        this.updateSelectionMenuLabel();
    }

    updateSelectionMenuLabel() {
        if (this.menuOptions && this.menuOptions.length > 0) {
            let versionMenuItem = this.menuOptions.find(x => x.title.indexOf("Version History") != -1);
            if (versionMenuItem) {
                versionMenuItem.title = 'Version History (' + (this.selection ? this.selection.VersionCount : 0) + ')';
            }
        }
        if (this.disabledMenu && this.disabledMenu.length > 0) {
            let versionMenuItem = this.disabledMenu.find(x => x.label.indexOf("Version History") != -1);
            if (versionMenuItem) {
                versionMenuItem.label = 'Version History (' + (this.selection ? this.selection.VersionCount : 0) + ')';
            }
        }
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
        if (this.previousSelectedNode && this.metrics && this.metrics.length > 0)
            this.selectedNode = { ...this.previousSelectedNode };
        if (this.previousSelection && this.metrics && this.metrics.length > 0)
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
};