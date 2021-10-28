import { Component, OnInit, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { WorkflowService } from '../../services/workflow.service';
import { Router } from '@angular/router';
import { map } from 'rxjs/operators';
import { CompanySettingsService } from '../../services/settings.service';

@Component({
    selector: 'd3s-monitor-list',
    template: ` 
<div>
    <d3s-loading *ngIf="isLoading" isLoading="true"></d3s-loading>
    <div *ngIf="!isLoading">
        <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..." class="grid-simple-filter">
        <p-table #dt [value]="workflowItems" selectionMode="single" [metaKeySelection]="true" [globalFilterFields]="['Name','ObjectTypeName','Status','Version']" [pageLinks]="3" [paginator]="true" [rows]="15" [rowsPerPageOptions]="defaultPagingOptions" [selection]="selection" (selectionChange)="selection = $event; selectionChange.emit($event)">
            <ng-template pTemplate="header">
                <tr>
                    <th [pSortableColumn]="'Name'">
                        Workflow Name
                        <d3s-sortIcon [field]="'Name'"></d3s-sortIcon>
                    </th>
                    <th [pSortableColumn]="'ObjectTypeName'">
                        Type Name
                        <d3s-sortIcon [field]="'ObjectTypeName'"></d3s-sortIcon>
                    </th>
                    <th [pSortableColumn]="'Status'">
                        Status
                        <d3s-sortIcon [field]="'Status'"></d3s-sortIcon>
                    </th>
                    <th [pSortableColumn]="'Version'">
                        Version
                        <d3s-sortIcon [field]="'Version'"></d3s-sortIcon>
                    </th>
                     <th style="width: 30px"></th>
                </tr>
                <tr [hidden]="showSimpleFilter">
                    <th><d3s-column-filter [field]="'Name'" [datatype]="'text'"></d3s-column-filter></th>
                    <th><d3s-column-filter [field]="'ObjectTypeName'" [datatype]="'text'"></d3s-column-filter></th>
                    <th><d3s-column-filter [field]="'Status'" [datatype]="'text'"></d3s-column-filter></th>
                    <th><d3s-column-filter [field]="'Version'" [datatype]="'text'"></d3s-column-filter></th>
                    <th></th>
                </tr>
            </ng-template>
            <ng-template pTemplate="body" let-item>
                <tr [pSelectableRow]="item">
                    <td>{{item.Name}}</td>
                    <td>{{item.ObjectTypeName}}</td>
                    <td>{{item.Status}}</td>
                    <td>{{item.Version}}</td>
                    <td>
                        <d3s-preview-tooltip objectType="WorkflowVersion" [objectId]="item.VersionID" icon="info"></d3s-preview-tooltip>
                    </td>
                </tr>
            </ng-template>
            <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
            </ng-template>
        </p-table>
    </div>     
</div>
              `,
    providers: [WorkflowService],
})

export class MonitorListComponent extends BaseComponent implements OnInit, OnChanges {

    @Input() workflowTypes: any[];
    @Input() selection: any;
    @Input() showSimpleFilter: boolean;
    @Output() selectionChange = new EventEmitter();
    @Input() objectType: string;
    @Input() objectId: number;
    @Output() filteredTypes = new EventEmitter();
    @Output() onLoadComplete = new EventEmitter();
    
    useFilteredObject: boolean = false;
    workflowItems: any[];

    constructor(
        protected settingsService: CompanySettingsService,
        protected workflowService: WorkflowService,
        protected router: Router) {
        super(settingsService);
    }

    ngOnInit() {
        this.load();
    }

    ngOnChanges(changes: SimpleChanges) {
        if (!(changes['showSimpleFilter'] && changes['showSimpleFilter'].currentValue != changes['showSimpleFilter'].previousValue))
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
            .pipe(
                 map(r => {
                    this.workflowItems = r;
                    //console.log(this.useFilteredObject, this.objectType, this.objectId, this.workflowItems);
                    r.forEach(i => {
                        if (i.ResponsibleUser != null && i.ResponsibleUser.constructor === Array) {
                            i.ResponsibleUser = i.ResponsibleUser[0];
                        }
                    });
                }),
                map(() => {
                    if (this.objectType != null && this.objectId != null) {
                        //artifact type
                        if (this.objectType.toLowerCase().endsWith('type')) {
                            this.workflowItems = this.workflowItems.filter(i => i.Object == this.objectType && i.ObjectID == this.objectId);
                        } else if (this.useFilteredObject) {
                            //filtering is done on the server for specific objects. If the list comes back null, the specific object is not present
                            this.workflowItems = this.workflowItems.filter((i) => i.ObjectNames != null);
                        }
                    }
                }),
                map(() => {
                    let filteredTypeList = [];
                    if (this.workflowItems != null) {
                        this.workflowItems.forEach(w => filteredTypeList.push(w.TypeID));
                        this.filteredTypes.emit(filteredTypeList);
                    }

                }),
                map(() => {
                    if (this.workflowItems != null && this.workflowItems.length > 0) {
                        //select first row by default
                        this.selection = this.workflowItems[0];
                        this.selectionChange.emit(this.selection);
                    } 
                    this.onLoadComplete.emit({ rows: this.workflowItems == null ? 0 : this.workflowItems.length });
                    this.isLoading = false;
                })
            ).subscribe();
    }

    openItem(url: string) {
        this.router.navigateByUrl(url);
    }
}