import { Input, Component, EventEmitter, Output, OnInit, OnChanges, SimpleChange, ChangeDetectionStrategy, ChangeDetectorRef  } from '@angular/core';
import { MetricsService } from '../../../services/metrics.service';
import { MetricAssetViewModel, MetricFieldTypeViewModel, ScoreType, ScoreTypeAllocation } from '../../../models/metrics.model';
import { TreeNode, MenuItem } from 'primeng/api';
import { BaseComponent } from '../../shared/base.component';
import { FormMode } from '../../../models/form.model';
import { AssetTypeMetricModel } from '../../../models/asset.model';
import { FormHelpers } from '../../../static/form-helpers';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { AllocationService } from '../../../services/allocations.service';

@Component({
    selector: 'd3s-admin-metric-list',
    template: ` 
               <d3s-loading [isLoading]="isLoading"></d3s-loading>
               <div *ngIf="!isLoading">
                    <div *ngIf="metricTree.length == 0" class="empty-metric-message"><i class="fa fa-info-circle"></i> Create one or more measures to complete your score definition</div>
                   <div>
                       <p-treeTable [value]="metricTree" [style]="{'width': '95', 'line-height' : '25px' }" selectionMode="single" [selection]="selectedNode" (selectionChange)="selectNode($event)">
                           <ng-template pTemplate="header">
                               <tr> 
                                   <th>Name</th>
                                   <th style="width: 120px" *ngIf="!isExternallyCalculated">Weight</th>
                                   <th style="width: 120px">Effective Date</th>
                                   <th style="width: 40px"></th>
                                   <th style="width: 40px"></th>
                               </tr>
                           </ng-template>
                           <ng-template pTemplate="body" let-rowNode let-item="rowData">
                               <tr [ttSelectableRow]="rowNode">
                                   <td>
                                       <d3s-treeTableToggler [rowNode]="rowNode"></d3s-treeTableToggler>
                                       {{item.Name}}
                                   </td>
                                   <td *ngIf="!isExternallyCalculated">{{getAsPrecentage(item.Weight)}}</td>
                                   <td>{{item.EffectiveDate | utcDate | date:'shortDate'}}</td>
                                   <td>
                                       <div class="RowTools" *ngIf="item.IsGroup">             
                                           <button class="rowtool-button-top" igButton icon="fa-plus" (click)="selectNode(rowNode.node); add(true)" tooltip="Add measure to group"></button>
                                       </div>
                                   </td>
                                   <td>
                                       <div class="RowTools">  
                                           <p-menu #cardmenu [popup]="true" [model]="getCardMenuItems()" appendTo="body" styleClass="kebabmenu yellow-items"></p-menu>
                                           <button class="rowtool-button-top" igButton icon="fa-ellipsis-v" (click)="selectNode(rowNode.node);cardmenu.toggle($event);" tooltip="Measure Actions"></button>                                
                                       </div>
                                   </td>
                               </tr>
                           </ng-template>
                       </p-treeTable>
                       <div *ngIf="metricTree.length == 0" class="no-measure-message">No measures defined</div>
                   </div>
                   <d3s-modal [title]="'Create Measure'" additionalClasses="large-dialog modal-measure" (onClose)="formMode = FormMode.Default;" (onSave)="formMode = FormMode.Default; load(); " [isVisible]="formMode == FormMode.Adding">
                       <d3s-admin-metric-editor [isExternallyCalculated]="isExternallyCalculated" [allocationUid]="allocationUid" [metricEditorFieldTypes]="metricListFieldTypes" [parentUid]="selection?.Uid" (onCancel)="formMode = FormMode.Default;" (onSave)="formMode = FormMode.Default; load(); "></d3s-admin-metric-editor>
                   </d3s-modal>
                   <d3s-modal [title]="'Edit Measure'" additionalClasses="large-dialog modal-measure" (onClose)="formMode = FormMode.Default;" (onSave)="formMode = FormMode.Default; load(); " [isVisible]="formMode == FormMode.Editing">
                       <d3s-admin-metric-editor [isExternallyCalculated]="isExternallyCalculated" [allocationUid]="allocationUid" [(model)]="selection" [metricEditorFieldTypes]="metricListFieldTypes" [uid]="selection?.Uid" (onCancel)="formMode = FormMode.Default; load();" (onSave)="formMode = FormMode.Default; load(); "></d3s-admin-metric-editor>
                   </d3s-modal>
                   <div *ngIf="formmode == FormMode.Deleting">
                       <header>
                           Delete Group
                       </header>
                       <d3s-delete-form
                           [uri]="'api/v2/metrics/' + selection?.Uid"
                           [method]="'delete'"
                           [prompt]="'Are you sure you want to delete the metric group [' + [selection?.Name] + ']?'"                                         
                           (onCancel)="formMode = FormMode.Default"
                           (onDeleteSuccess)="formMode = FormMode.Default; load();"
                           (onDeleteFail)="formMode = FormMode.Default">
                       </d3s-delete-form> 
                   </div>
                </div>
                `,
    providers: [MetricsService, AllocationService]
})

export class AdminMetricListComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() assetType: AssetTypeMetricModel;
    @Input() allocationUid: string;
    @Output() selectionChange = new EventEmitter();

    private metrics: MetricAssetViewModel[] = [];
    private metricTree: TreeNode[] = [];
    private selectedNode: TreeNode;
    private selection: MetricAssetViewModel;

    @Input() metricListFieldTypes: MetricFieldTypeViewModel[] = [];

    private formMode = FormMode.Default;
    FormMode = FormMode;

    private isExternallyCalculated: boolean = false;

    constructor(private metricsService: MetricsService, private allocationService: AllocationService, protected messagesService: MessagesObservableService) {
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
                        if (this.metricTree !== null && this.metricTree.length > 0) {
                            this.selection = this.metricTree[0].data;
                            this.selectionChange.emit(this.selection);
                            this.selectedNode = this.metricTree[0];
                        }
                    }

                    this.allocationService.getAllocationsByAssetTypeUid(this.assetType.Uid).subscribe(res => {
                        this.isLoading = false;
                        this.isExternallyCalculated = res.find(x => x.uid === this.allocationUid).isExternallyCalculated;
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
        if (!asChild) {
            this.selection = null;
            this.selectedNode = null;
            this.selectionChange.emit(this.selection);
        }
        this.formMode = FormMode.Adding;
    }

    public edit() {
        this.formMode = FormMode.Editing;
    }

    public delete() {
        this.formMode = FormMode.Deleting;
    }
    public close() {
        this.formMode = FormMode.Default;
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
            label: 'Version History',
            command: (event) => { console.log("not yet implemented"); }
        });
        return menu;
    }
};