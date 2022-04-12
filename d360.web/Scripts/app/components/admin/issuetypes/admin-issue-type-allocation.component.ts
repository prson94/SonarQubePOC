import { Component, Input, OnChanges, SimpleChange, SimpleChanges } from '@angular/core';
import { AdminBaseComponent } from '../admin-base.component'
import { WorkflowService } from '../../../services/workflow.service';
import { BaseComponent } from '../../shared/base.component';
import { FormMode } from '../../../models/form.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { AssetTypeClass } from '../../../models/asset.model';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-admin-issue-type-allocation',
    template: `
                <div class="row"> 
                <header *ngIf="formMode == FormMode.Default"><ng-container i18n>Allocations</ng-container>
                    <d3s-tile-actions [hasAdd]="true" (addClick)="add()" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
                </header>
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <div *ngIf="!isLoading">
                        <div [ngSwitch]="formMode">
                            <div *ngSwitchCase="FormMode.Default" class="col s12">
                                <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="searchText" class="grid-simple-filter">
                                <p-table #dt [value]="allocations" selectionMode="single" [metaKeySelection]="true" [globalFilterFields]="['ClassName','Path']" [pageLinks]="3" [paginator]="true" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions" [(selection)]="selection">
                                    <ng-template pTemplate="header">
                                        <tr>
                                            <th style="width: 150px" [pSortableColumn]="'ClassName'">
                                                <ng-container i18n>Class</ng-container>
                                                <d3s-sortIcon [field]="'ClassName'"></d3s-sortIcon>
                                            </th>
                                            <th [pSortableColumn]="'Path'">
                                                <ng-container i18n>Object Name</ng-container>
                                                <d3s-sortIcon [field]="'Path'"></d3s-sortIcon>
                                            </th>
                                            <th *ngIf="showResponsibilities">
                                                <ng-container i18n>Responsibilities</ng-container>
                                            </th>
                                            <th style="width: 40px"></th>
                                            <th style="width: 40px"></th>
                                        </tr>
                                        <tr [hidden]="showSimpleFilter">
                                            <th><d3s-column-filter [field]="'ClassName'" [datatype]="'text'"></d3s-column-filter></th>
                                            <th><d3s-column-filter [field]="'Path'" [datatype]="'text'"></d3s-column-filter></th>
                                            <th *ngIf="showResponsibilities"></th>
                                            <th></th>
                                            <th></th>
                                        </tr>
                                    </ng-template>
                                    <ng-template pTemplate="body" let-item>
                                        <tr [pSelectableRow]="item">
                                            <td>{{parseClassName(item.ClassName)}}</td>
                                            <td>{{item.Path}}</td>
                                            <td *ngIf="showResponsibilities">
                                                <ul *ngFor="let responsibility of item.Responsibilities" style="padding: 0;">
                                                    <li style="list-style-type:none;">{{ responsibility.Name }}</li>
                                                </ul>
                                            </td>
                                            <td>
                                                <div class="RowTools">
                                                    <a style="cursor:pointer;" (click)="selection = item; formMode = FormMode.Adding"><i class="fa fa-pencil"></i></a>
                                                </div>
                                            </td>
                                            <td>
                                                <div class="RowTools">
                                                    <a style="cursor:pointer;" (click)="selection = item; formMode = FormMode.Deleting"><i class="fa fa-trash-o"></i></a>
                                                </div>
                                            </td>
                                        </tr>
                                    </ng-template>
                                    <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                                        <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                                    </ng-template>
                                </p-table>                 
                            </div>
                            <div *ngSwitchCase="FormMode.Adding" class="col s12">
                                <d3s-admin-issue-type-allocation-editor 
                                    [issueTypeUid] = "issueTypeUid" 
                                    [allocation]="selection" 
                                    [allocations]="allocations"
                                    (closeClick)="editorClose()">
                                </d3s-admin-issue-type-allocation-editor>
                            </div>
                            <div *ngSwitchCase="FormMode.Deleting" class="col s12">
                                <d3s-delete-form
                                            [callback]="deleteCallback"
                                            method="callback"
                                            [prompt]="deleteModalTitle"                                         
                                            (onCancel)="formMode = FormMode.Default"
                                ></d3s-delete-form> 
                            </div>
                        </div>
                    </div>
                </div>  
                `,
    providers: [WorkflowService],
})

export class AdminIssueTypeAllocationComponent extends BaseComponent implements OnChanges {
    @Input() issueTypeUid: string;
    assetTypeClass = AssetTypeClass;
    formMode = FormMode.Default;
    FormMode = FormMode;
    allocations = [];
    selection = null;
    deleteCallback: Function;
    showResponsibilities: boolean;

    searchText = $localize`Search...`;
    deleteModalTitle = $localize`Are you sure you want to delete this allocation?`;

    constructor(
        protected messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        private workflowService: WorkflowService
    ) {
        super(settingsService);
        this.deleteCallback = this.delete.bind(this);
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes['issueTypeUid'].currentValue != changes['issueTypeUid'].previousValue || changes['issueTypeUid'].isFirstChange) {
            this.formMode = FormMode.Default;
            this.load();
        }
    }

    load() {
        if (this.issueTypeUid == null) {
            this.allocations = [];
            return;
        }
        this.isLoading = true;
        this.workflowService.getIssueTypeAllocations(this.issueTypeUid)
            .subscribe(r => {
                this.allocations = r;
                this.showResponsibilities = this.allocations.some((a) => a.Responsibilities && a.Responsibilities.length > 0);
                this.isLoading = false;
            });
    }

    add() {
        this.selection = null;
        this.formMode = FormMode.Adding;
    }

    delete() {
        this.isLoading = true;
        this.workflowService.deleteIssueTypeAllocation(this.issueTypeUid, this.selection.AssetTypeUid)
            .subscribe(r => {
                this.isLoading = false;
                this.formMode = FormMode.Default;
                this.showMessageForResult(this.messagesService, r);
                this.load();
            });
    }

    editorClose() {
        this.formMode = FormMode.Default;
        this.load();
    }

    parseClassName(className: string) {
        var name = className;
        switch (className) {
            case "BusinessAsset":
                name = $localize`Business Asset`;
                break;
            case "TechnicalAsset":
                name = $localize`Technical Asset`;
                break;
            case "DiagramAsset":
                name = $localize`Diagram Asset`;
                break;
            case "ReferenceItemType":
                name = $localize`Reference Item Type`;
                break;
        }
        return name;
    }
}
