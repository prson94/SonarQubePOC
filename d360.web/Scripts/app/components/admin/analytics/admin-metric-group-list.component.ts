import { Input, Component, EventEmitter, Output, OnInit } from '@angular/core';
import { MetricsService } from '../../../services/metrics.service';
import { Group } from '../../../models/metrics.model';
import { TreeNode } from 'primeng/primeng';
import { BaseComponent } from '../../shared/base.component';
import { FormMode } from '../../../models/form.model';
import { MessagesService } from '../../../services/messages.service';

@Component({
    selector: 'd3s-admin-metric-group-list',
    template: ` 
               <header *ngIf="formMode == FormMode.Default">
                    Groups
                    <d3s-tile-actions hasAdd="true" (addClick)="selectNode(null); add()"></d3s-tile-actions>
                </header>
               <d3s-loading [isLoading]="isLoading"></d3s-loading>
               <div *ngIf="!isLoading">
                <div [ngSwitch]="formMode">
                    <div *ngSwitchCase="FormMode.Default">
                        <p-treeTable [value]="groupTree" [style]="{'width': '95', 'line-height' : '25px' }" selectionMode="single" [selection]="selectedNode" (selectionChange)="selectNode($event)">
                            <p-column field="Name" header="Name"></p-column>
                            <p-column field="Weight" header="Weight"></p-column>
                            <p-column  [style]="{width:'40px'}">
                                <ng-template let-node="rowData" pTemplate type="body">
                                    <div class="RowTools">                                
                                        <a style="cursor:pointer;" (click)="selectNode(node); add()"><i class="fa fa-plus"></i></a>                                    
                                    </div>
                                </ng-template>
                            </p-column> 
                            <p-column  [style]="{width:'40px'}">
                                <ng-template let-node="rowData" pTemplate type="body">
                                    <div class="RowTools">                                
                                        <a style="cursor:pointer;" (click)="selectNode(node); edit()"><i class="fa fa-pencil"></i></a>                                    
                                    </div>
                                </ng-template>
                            </p-column> 
                            <p-column  [style]="{width:'40px'}">
                                <ng-template let-node="rowData" pTemplate type="body">
                                    <div class="RowTools">                                
                                        <a style="cursor:pointer;" (click)="selectNode(node); delete()"><i class="fa fa-trash-o"></i></a>                                    
                                    </div>
                                </ng-template>
                            </p-column> 
                        </p-treeTable>
                    </div>
                    <div *ngSwitchCase="FormMode.Adding">
                        <d3s-admin-metric-group-editor [parentId]="selection?.ID" (onCancel)="formMode = FormMode.Default" (onSave)="formMode = FormMode.Default; load(); "></d3s-admin-metric-group-editor>
                    </div>
                    <div *ngSwitchCase="FormMode.Editing">
                        <d3s-admin-metric-group-editor [groupId]="selection.ID" (onCancel)="formMode = FormMode.Default" (onSave)="formMode = FormMode.Default; load(); "></d3s-admin-metric-group-editor>
                    </div>
                    <div *ngSwitchCase="FormMode.Deleting">
                        <header>
                            Delete Group
                        </header>
                        <d3s-delete-form
                            [uri]="'form/MetricGroup?id=' + selection?.ID"
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

export class AdminMetricGroupListComponent extends BaseComponent implements OnInit {
    @Output() selectionChange = new EventEmitter();
    private groups: Group[] = [];
    private groupTree: TreeNode[] = [];
    private selectedNode: TreeNode;
    private selection: Group;
    private formMode = FormMode.Default;
    FormMode = FormMode;

    constructor(private metricsService: MetricsService, protected messagesService: MessagesService) {
        super();
    }

    ngOnInit() {
        this.load();
    }
      

    load() {
        //console.log('load group list');
        this.isLoading = true;
        this.groups = [];
        this.groupTree = [];
        this.metricsService.getGroups()
            .then(r => {

                this.groups = r;
                this.groups.filter(g => g.ParentID == null).forEach(g => {
                    let n = {
                        data: g,
                        children: [],
                        expanded: true
                    }
                    this.groupTree.push(n);
                    this.addChildren(n);
                });
                this.isLoading = false;
            });
    }

    addChildren(node: TreeNode) {
        let children = this.groups.filter(g => g.ParentID == node.data.ID);
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