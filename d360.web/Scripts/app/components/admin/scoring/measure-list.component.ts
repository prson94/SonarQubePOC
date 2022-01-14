import { Input, Component, EventEmitter, Output, OnInit, OnChanges, SimpleChange, ViewEncapsulation } from '@angular/core';
import { MetricsService } from '../../../services/metrics.service';
import { MetricAssetViewModel, ScoreTypeAllocation, ScoreType } from '../../../models/metrics.model';
import { TreeNode } from 'primeng/api';
import { BaseComponent } from '../../shared/base.component';
import { FormMode } from '../../../models/form.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { AllocationService } from '../../../services/allocations.service';
import * as _ from 'lodash';
import { AssetTypeMetricModel } from '../../../models/asset.model';
import { CommonScreenReferencesModel } from './common-screen-references-model';
import { CompanySettingsService } from '../../../services/settings.service';
import { AppSettingsEnum } from '../../../models/settings.model';
import { ScoreService } from '../../../services/score.service';

@Component({
    selector: 'measure-list',
    templateUrl: './measure-list.component.html',
    providers: [MetricsService, AllocationService],
    styles: [
        `
        .ig-badge.default {
            margin-top: 1px; 
        }
        .action-box p-checkbox{
            margin-right: 32px; 
        }
        .badge-container{
            margin-left: 16px;
            max-height: 24px;
            display: inline-flex;
            margin-top: 1px;
        }
        `
    ],
    encapsulation: ViewEncapsulation.None
})

export class MeasureListComponent extends BaseComponent implements OnInit, OnChanges {
    @Output() selectionChange = new EventEmitter();
    @Input() assetType: AssetTypeMetricModel;
    @Input() allocation: ScoreTypeAllocation;
    @Input() maxScoreEffectiveDate: Date;

    @Input() screenReferences: CommonScreenReferencesModel;

    @Input() showDisabled: boolean = false;

    helpUri: string = "";

    private metrics: MetricAssetViewModel[] = [];
    private metricTree: TreeNode[] = [];
    private selectedNode: TreeNode;
    private selection: MetricAssetViewModel;
    private previousSelection: MetricAssetViewModel;
    private previousSelectedNode: TreeNode;

    private formMode = FormMode.Default;
    FormMode = FormMode;

    showDelete: boolean = false;

    private isHistoryModalVisible: boolean = false;
    private isRecalculateModalVisible: boolean = false;
    private isCallingRecalculate: boolean = false;

    todayAndEffectiveDateAreSame(item: MetricAssetViewModel): boolean {
        if (item) {
            let today = new Date();
            let todayMs = Date.UTC(today.getUTCFullYear(), today.getUTCMonth(), today.getUTCDate());
            let effectiveDate = new Date(item.EffectiveDate);
            let effectiveDateMs = effectiveDate.getTime();
            return (effectiveDateMs == todayMs);
        }
        else {
            return false;
        }
    }

    menuClicked($event) {
        switch ($event.value) {
            case 'Edit': this.edit();
                break;
            case 'Disable': this.delete();
                break;
            case 'Recalculate': this.showRecalculate(true);
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
        },
        {
            "title": "Recalculate"
        }
    ];
    private disabledMenu = [
        {
            "title": "Version History"
        }
    ];

    constructor(
        private metricsService: MetricsService,
        private allocationService: AllocationService,
        protected messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        private scoreService: ScoreService    ) {
        super(settingsService);

        let helpBaseUri: string = this.settingsService.getAppSetting(AppSettingsEnum.HelpBaseUri);
        this.helpUri = helpBaseUri + "Default.htm#d-admin/scoring-definitions.htm?TocPath=Administration%257C_____4";
    }

    delayedReload = _.debounce(() => {
        this.formMode = FormMode.Default;
        this.load();
    }, 200);

    ngOnInit() {
        this.delayedReload();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        let requiresLoad = false;
        if (changes['allocation'] && this.allocation) {
            requiresLoad = true;
        }
        if (changes['showDisabled'] != null || changes['showDisabled'] != undefined) {
            requiresLoad = true;
        }

        if (requiresLoad) {
            this.delayedReload();
        }
    }

    load(initiallySelected?: string) {
        this.isLoading = true;
        this.metrics = [];
        this.metricTree = [];
        if (this.allocation.uid) {
            this.metricsService.getMetricsByAllocation(this.allocation.uid, this.showDisabled)
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
                    this.allocationService.getAllocationsByAssetTypeUid(this.allocation.assetTypeUid).subscribe(res => {
                        this.isLoading = false;
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
                    this.isLoading = false;
                });
        }
        else {
            this.selection = null;
            this.metricTree = [];
        }

    }

    addChildren(node: TreeNode) {
        if (this.metrics) {
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
    }

    public selectNode(e: any) {
        if (e == null)
            return;
        this.selectedNode = e;
        this.selection = e === null ? null : { ...e.data };
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
            let versionMenuItem = this.disabledMenu.find(x => x.title.indexOf("Version History") != -1);
            if (versionMenuItem) {
                versionMenuItem.title = 'Version History (' + (this.selection ? this.selection.VersionCount : 0) + ')';
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

    //#region Recalculate

    public cancelRecalculate() {
        this.showRecalculate(false);
    }

    public recalculate() {
        this.isCallingRecalculate = true;
        this.scoreService.recalculateMeasure(this.allocation.uid, this.selection.Uid).subscribe((returnValue) => {
            this.isCallingRecalculate = false;
            this.showRecalculate(false);
        });
    }

    public showRecalculate(isRecalculateVisible: boolean) {
        this.isRecalculateModalVisible = isRecalculateVisible;
    }

    //#endregion

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

    isDataQualityScoreType() {
        return (this.allocation.scoreType == ScoreType.DataQuality || ScoreType[this.allocation.scoreType.toString()] == ScoreType.DataQuality) && !this.allocation.isExternallyCalculated;
    }
    isExternalScoreType() {
        return this.allocation.isExternallyCalculated;
    }
    isGovernanceScoreType() {
        return (this.allocation.scoreType == ScoreType.Governance || ScoreType[this.allocation.scoreType.toString()] == ScoreType.Governance) && !this.allocation.isExternallyCalculated;
    }

    showRulePathsError() {
        return this.isDataQualityScoreType() && ((this.screenReferences.paths && this.screenReferences.paths.length == 0) || !this.screenReferences.paths); 
    }

    getSelectedRuleResultPath() {
        let html = ';'
        const ruleResultPathUid = this.selection?.Definition.DataQuality.ResultPathUid;
        if (ruleResultPathUid && this.screenReferences && this.screenReferences.paths) {
            const matches = this.screenReferences.paths.filter(p => { return p.value == ruleResultPathUid; });
            if (matches.length > 0) {
                html = matches[0].label;
            }
        }
        return html;
    }
}