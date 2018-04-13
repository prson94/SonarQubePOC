import { Component, OnInit, Input, Output, EventEmitter, OnChanges } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { WorkflowService } from '../../services/workflow.service';
import { WorkflowListItem } from '../../models/workflow.model';
import { Router } from '@angular/router';


@Component({
    selector: 'd3s-monitor-list',
    template: ` 
<div class="tile tile-detail">
    <d3s-loading *ngIf="isLoading" isLoading="true"></d3s-loading>
    <div *ngIf="!isLoading">
        <header>
            Workflows
            <d3s-tile-actions [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>
        </header>
        <input [hidden]="!showSimpleFilter" #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">                                              
        <p-dataTable #dt [globalFilter]="gb" [value]="workflowItems" selectionMode="single" [rows]="15" [rowsPerPageOptions]="defaultPagingOptions" [paginator]="true" [pageLinks]="3" [selection]="selection" (selectionChange)="selection = $event; selectionChange.emit($event)">
            <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>            
            <p-column field="Name" header="Name" sortable="true" [filter]="!showSimpleFilter" filterMatchMode="contains"></p-column>
            <p-column field="ObjectTypeName" header="Type" sortable="true" [filter]="!showSimpleFilter"  filterMatchMode="contains"></p-column>  
            <p-column field="ObjectNames" header="Objects" sortable="true" [filter]="!showSimpleFilter" filterMatchMode="contains">
                <ng-template pTemplate="body" let-item="rowData">
                    <span *ngIf="item.ObjectNames != null && item.ObjectNames.length > 15" [pTooltip]="item.ObjectNames" style="word-wrap:break-word;">{{item.ObjectNames | slice:0:15}}...</span>
                    <span *ngIf="item.ObjectNames != null && item.ObjectNames.length <= 15" style="word-wrap:break-word;">{{item.ObjectNames}}</span>
                </ng-template>
            </p-column>    
            <p-column header="Status" field="Status" sortable="true" [filter]="!showSimpleFilter" filterMatchMode="contains"></p-column>  
            <p-column field="ResponsibleUser" header="Responsibility" sortable="true" [filter]="!showSimpleFilter" [style]="{'width':'120px'}" filterMatchMode="contains">
                <ng-template pTemplate="body" let-item="rowData">
                    <span *ngIf="item.ResponsibleUser != null && item.ResponsibleUser.length > 15" [pTooltip]="item.ResponsibleUser" style="word-wrap:break-word;">{{item.ResponsibleUser | slice:0:15}}...</span>
                    <span *ngIf="item.ResponsibleUser != null && item.ResponsibleUser.length <= 15" style="word-wrap:break-word;">{{item.ResponsibleUser}}</span>
                </ng-template>
            </p-column>
        </p-dataTable>
    </div>     
</div>
              `,
    providers: [WorkflowService],
})

export class MonitorListComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() workflowTypes: any[];
    @Input() selection: any;
    @Output() selectionChange = new EventEmitter();
    @Input() objectType: string;
    @Input() objectId: number;
    @Output() filteredTypes = new EventEmitter();
    @Output() onLoadComplete = new EventEmitter();
    
    useFilteredObject: boolean = false;
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
            this.useFilteredObject = false;
            this.selection = null;
            this.selectionChange.emit(null);
            this.filteredTypes.emit(null);
            return;
        }

        this.useFilteredObject = (this.objectType != null && this.objectId != null && !this.objectType.toLowerCase().endsWith('type'));


        this.isLoading = true;
        let typeList = "";
        this.workflowTypes.forEach(s => typeList += s.toString() + ',');
        this.workflowService.getWorkflowsByTypeList(typeList, this.useFilteredObject ? this.objectType : null, this.useFilteredObject ? this.objectId : null)
            .then(r => {
                this.workflowItems = r;
                //console.log(this.useFilteredObject, this.objectType, this.objectId, this.workflowItems);
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
                    } else if (this.useFilteredObject) {
                        //filtering is done on the server for specific objects. If the list comes back null, the specific object is not present
                        this.workflowItems = this.workflowItems.filter(i => i.ObjectNames != null);
                    }
                }
            })
            .then(() => {
                let filteredTypeList = [];
                if (this.workflowItems != null) {
                    this.workflowItems.forEach(w => filteredTypeList.push(w.TypeID));
                    this.filteredTypes.emit(filteredTypeList);
                }

            })
            .then(() => {
                if (this.workflowItems != null && this.workflowItems.length > 0) {
                    //select first row by default
                    this.selection = this.workflowItems[0];
                    this.selectionChange.emit(this.selection);
                }
                this.onLoadComplete.emit({ rows: this.workflowItems == null ? 0 : this.workflowItems.length });
                this.isLoading = false;
            });
    }

    openItem(url: string) {
        this.router.navigateByUrl(url);
    }
}