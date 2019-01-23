import { Input, Component, EventEmitter, Output, OnInit, OnChanges, SimpleChange, ChangeDetectionStrategy, ChangeDetectorRef  } from '@angular/core';
import { MetricsService } from '../../../services/metrics.service';
import { MetricAssetViewModel, MetricFieldTypeViewModel } from '../../../models/metrics.model';
import { TreeNode } from 'primeng/primeng';
import { BaseComponent } from '../../shared/base.component';
import { FormMode } from '../../../models/form.model';
import { MessagesService } from '../../../services/messages.service';
import { AssetTypeMetricModel } from '../../../models/asset.model';
import { FormHelpers } from '../../../static/form-helpers';

@Component({
    selector: 'd3s-admin-metric-list',
    template: ` 
               <header *ngIf="formMode == FormMode.Default">
                    Measures
                    <d3s-tile-actions hasAdd="true" (addClick)="selectNode(null); add()"></d3s-tile-actions>
                </header>
               <d3s-loading [isLoading]="isLoading"></d3s-loading>
               <div *ngIf="!isLoading">
                <div [ngSwitch]="formMode">
                    <div *ngSwitchCase="FormMode.Default">
                        <p-treeTable [value]="metricTree" [style]="{'width': '95', 'line-height' : '25px' }" selectionMode="single" [selection]="selectedNode" (selectionChange)="selectNode($event)">
                            <ng-template pTemplate="header">
                                <tr> 
                                    <th>Name</th>
                                    <th>Weight</th>
                                    <th>Effective Date</th>
                                    <th style="width: 40px"></th>
                                    <th style="width: 40px"></th>
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
                                    <td>{{item.Weight}}</td>
                                    <td>{{item.EffectiveDate | utcDate | date:'shortDate'}}</td>
                                    <td>
                                        <div class="RowTools" *ngIf="rowNode.node.data.Uid">                                
                                            <a style="cursor:pointer;" (click)="selectNode(rowNode.node);"><i [copy-clipboard]="rowNode.node.data.Uid" [pTooltip]="'UID: \n' + rowNode.node.data.Uid + '\n\n (click to copy)\n'" tooltipPosition="top" class="fa fa-info"></i></a>                                      
                                        </div>
                                    </td>
                                    <td>
                                        <div class="RowTools" *ngIf="item.IsGroup">                                
                                            <a style="cursor:pointer;" (click)="selectNode(rowNode.node); add()"><i class="fa fa-plus"></i></a>                                      
                                        </div>
                                    </td>
                                    <td>
                                        <div class="RowTools">                                
                                            <a style="cursor:pointer;" (click)="selectNode(rowNode.node); edit()"><i class="fa fa-pencil"></i></a>                                    
                                        </div>
                                    </td>
                                    <td>
                                        <div class="RowTools">                                
                                            <a style="cursor:pointer;" (click)="selectNode(rowNode.node); delete()"><i class="fa fa-trash-o"></i></a>                                    
                                        </div>
                                    </td>
                                </tr>
                            </ng-template>
                        </p-treeTable>
                    </div>
                    <div *ngSwitchCase="FormMode.Adding">
                        <d3s-admin-metric-editor [metricEditorFieldTypes]="metricListFieldTypes" [assetTypeUid]="assetType?.Uid" [parentUid]="selection?.Uid" (onCancel)="formMode = FormMode.Default;" (onSave)="formMode = FormMode.Default; load(); "></d3s-admin-metric-editor>
                    </div>
                    <div *ngSwitchCase="FormMode.Editing">
                        <d3s-admin-metric-editor [(model)]="selection" [metricEditorFieldTypes]="metricListFieldTypes" [assetTypeUid]="assetType?.Uid" [uid]="selection.Uid" (onCancel)="formMode = FormMode.Default; load();" (onSave)="formMode = FormMode.Default; load(); "></d3s-admin-metric-editor>
                    </div>
                    <div *ngSwitchCase="FormMode.Deleting">
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

                </div>
                `,
    providers: [MetricsService, MessagesService]
})

export class AdminMetricListComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() assetType: AssetTypeMetricModel;
    @Output() selectionChange = new EventEmitter();

    private metrics: MetricAssetViewModel[] = [];
    private metricTree: TreeNode[] = [];
    private selectedNode: TreeNode;
    private selection: MetricAssetViewModel;

    private metricListFieldTypes: MetricFieldTypeViewModel[] = [];

    private formMode = FormMode.Default;
    FormMode = FormMode;

    constructor(private metricsService: MetricsService, protected messagesService: MessagesService) {
        super();
    }

    ngOnInit() {
        //this.load(); 
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes['assetType'] && this.assetType) {
            this.formMode = FormMode.Default;
            this.load();
        }
    }

    load() {
        this.isLoading = true;
        this.metrics = [];
        this.metricTree = [];
        if (this.assetType) {
            this.metricsService.getMetricsByAssetType(this.assetType.Uid)
                .subscribe(r => {

                    this.metrics = r;
                    if (this.metrics) {
                        this.metrics.filter(g => g.ParentUid == null).forEach(g => {
                            let n = {
                                data: g,
                                children: [],
                                expanded: true
                            }
                            this.metricTree.push(n);
                            this.addChildren(n);
                        });
                        if (this.metricTree != null && this.metricTree.length > 0) {
                            this.selection = this.metricTree[0].data;
                            this.selectionChange.emit(this.selection);
                        }
                    }

                    this.metricsService.getFieldTypeViewModelsByAssetType(this.assetType.Uid)
                        .subscribe(f => {
                            this.metricListFieldTypes = f;
                            this.isLoading = false;
                        });
                });
        }
        else {
            this.selection = null;
            this.metricTree = [];
        }
    }

    addChildren(node: TreeNode) {
        let children = this.metrics.filter(g => g.ParentUid == node.data.Uid);
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

    selectNode(e: any) {
        this.selectedNode = e;
        this.selection = e == null ? null : e.data;
        this.selectionChange.emit(this.selection);
    }

    add() {
        this.formMode = FormMode.Adding;
    }

    edit() {
        this.formMode = FormMode.Editing;
    }

    delete() {
        this.formMode = FormMode.Deleting;
    }

};