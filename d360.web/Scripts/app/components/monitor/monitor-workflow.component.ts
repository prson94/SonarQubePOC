import { Component, OnInit, Input, Output, EventEmitter, OnChanges } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { WorkflowService } from '../../services/workflow.service';
import { WorkflowListItem } from '../../models/workflow.model';
import { Router } from '@angular/router';


@Component({
    selector: 'd3s-monitor-workflow',
    template: ` 
<d3s-loading [isLoading]="isLoading"></d3s-loading>
<div *ngIf="!isLoading">
    <div class="tile tile-detail">
        <header>
            Workflows
            <d3s-tile-actions [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>
        </header>
        <input [hidden]="!showSimpleFilter" #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">                                              
        <p-dataTable #dt [globalFilter]="gb" [value]="workflowItems" selectionMode="single" [rows]="15" [rowsPerPageOptions]="[10,15,25]" [paginator]="true" [pageLinks]="3" [selection]="selection" (selectionChange)="selection = $event; selectionChange.emit($event)">
            <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>            
            <p-column field="Name" header="Name" sortable="true" [filter]="!showSimpleFilter" filterMatchMode="contains"></p-column>
            <p-column field="ObjectTypeName" header="Type"sortable="true" [filter]="!showSimpleFilter"  filterMatchMode="contains">
                <ng-template pTemplate="body" let-item="rowData">
                    <a (click)="openItem(item.NgUrl)">{{item.ObjectTypeName}}</a>
                </ng-template>
            </p-column>  
            <p-column field="ObjectNames" header="Objects"sortable="true" [filter]="!showSimpleFilter" filterMatchMode="contains">
                <ng-template pTemplate="body" let-item="rowData">
                    <a *ngIf="item.ObjectNames != null && item.ObjectNames.length > 15" [pTooltip]="item.ObjectNames" style="word-wrap:break-word;">{{item.ObjectNames | slice:0:15}}...</a>
                    <a *ngIf="item.ObjectNames != null && item.ObjectNames.length <= 15" style="word-wrap:break-word;">{{item.ObjectNames}}</a>
                </ng-template>
            </p-column>    
            <p-column header="Status" field="Status" sortable="true" [filter]="!showSimpleFilter" filterMatchMode="contains"></p-column>      
            <p-column field="UpdatedOn" header="Updated On" sortable="true" [filter]="!showSimpleFilter" filterMatchMode="contains">
                <ng-template pTemplate="body" let-item="rowData">
                    {{item.UpdatedOn | date:'shortDate'}}
                </ng-template>
            </p-column>
            <p-column field="UpdatedBy" header="Updated By" sortable="true" [filter]="!showSimpleFilter" filterMatchMode="contains"></p-column>
            <p-column field="VersionName" header="Version" sortable="true" [filter]="!showSimpleFilter" filterMatchMode="contains"></p-column>
            <p-column field="ResponsibleUser" header="Responsibility" sortable="true" [filter]="!showSimpleFilter" [style]="{'width':'120px'}" filterMatchMode="contains">
               <ng-template pTemplate="body" let-item="rowData">
                    <a *ngIf="item.ResponsibleUser != null && item.ResponsibleUser.length > 15" [pTooltip]="item.ResponsibleUser" style="word-wrap:break-word;">{{item.ResponsibleUser | slice:0:15}}...</a>
                    <a *ngIf="item.ResponsibleUser != null && item.ResponsibleUser.length <= 15" style="word-wrap:break-word;">{{item.ResponsibleUser}}</a>
                </ng-template>
            </p-column>
        </p-dataTable>
    </div>
</div>
              `,
    providers: [WorkflowService],
})

export class MonitorWorkflowComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() workflowTypes: any[];
    @Input() selection: any;
    @Output() selectionChange = new EventEmitter();
    @Input() objectType: string;
    @Input() objectId: number;

    workflowItems: any[];

    constructor(protected workflowService: WorkflowService, protected router: Router) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    ngOnChanges() {
        this.load();
    }

    private load() {
        if (this.workflowTypes == null || this.workflowTypes.length < 1) {
            this.workflowItems = [];
            this.selection = null;
            this.selectionChange.emit(null);
            return;
        }

        this.isLoading = true;
        let typeList = "";
        this.workflowTypes.forEach(s => typeList += s.toString() + ',');
        this.workflowService.getWorkflowsByTypeList(typeList)
            .then(r => {
                this.workflowItems = r;
                r.forEach(i => {
                    if (i.ResponsibleUser != null && i.ResponsibleUser.constructor === Array) {
                        i.ResponsibleUser = i.ResponsibleUser[0];
                    }
                });
            })
            .then(() => {
                if (this.objectType != null && this.objectId != null) {
                    //artifact type
                    if (this.objectType.toLowerCase().endsWith('type')) {
                        this.workflowItems = this.workflowItems.filter(i => i.ObjectType == this.objectType && i.ObjectTypeID == this.objectId);
                    } else {
                    //artifact
                        let item = this.objectType + '|' + this.objectId.toString();
                        this.workflowItems = this.workflowItems.filter(i => i.Objects != null && i.Objects.indexOf(item) > -1);
                    }
                }
            })
            .then(() => this.isLoading = false);
    }

    openItem(url: string) {
        this.router.navigateByUrl(url);
    }
}
