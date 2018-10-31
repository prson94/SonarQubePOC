import { Component, OnInit, Input, Output, EventEmitter, OnChanges, SimpleChange } from '@angular/core';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SurveysService } from '../../../services/surveys.service';
import { MessagesService } from '../../../services/messages.service';
import { BaseComponent } from '../../shared/base.component';
import { CustomAPIService } from '../../../services/custom-api.service';
import { ApiService, ApiEndpoint, ApiVersion, ApiField } from '../../../models/custom-api.model';
import { Router, ActivatedRoute } from '@angular/router';

@Component({
    selector: 'd3s-admin-api-endpoint-version-fields',
    providers: [CustomAPIService],
    template: `                                 
                        <div class="tile tile-detail">
                            <header *ngIf="!showEditor && !showDelete && !showDelete">Fields for v{{version?.MajorVersion}}.{{version?.MinorVersion}}
                            <d3s-tile-actions [hasAdd]="true" (addClick)="selected=null;showEditor=true;" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
                            </header>
                            <d3s-loading [isLoading]="isLoading"></d3s-loading>
                            <span *ngIf="!isLoading && !showDelete && !showEditor">
                                <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..." class="grid-simple-filter">
                                <p-table #dt [value]="fields" selectionMode="single" [globalFilterFields]="['Name','Type','AllowFilter','AllowSelect','AllowSort']" [pageLinks]="3" [paginator]="true" [rows]="10" [(selection)]="selected">
                                    <ng-template pTemplate="header">
                                        <tr>
                                            <th [pSortableColumn]="'Name'">
                                                Name
                                                <d3s-sortIcon [field]="'Name'"></d3s-sortIcon>
                                            </th>
                                            <th [pSortableColumn]="'Type'">
                                                DataType
                                                <d3s-sortIcon [field]="'Type'"></d3s-sortIcon>
                                            </th>
                                            <th [pSortableColumn]="'AllowFilter'">
                                                Filter?
                                                <d3s-sortIcon [field]="'AllowFilter'"></d3s-sortIcon>
                                            </th>
                                            <th [pSortableColumn]="'AllowSelect'">
                                                Select?
                                                <d3s-sortIcon [field]="'AllowSelect'"></d3s-sortIcon>
                                            </th>
                                            <th [pSortableColumn]="'AllowSort'">
                                                Sort?
                                                <d3s-sortIcon [field]="'AllowSort'"></d3s-sortIcon>
                                            </th>
                                            <th style="width: 35px"></th>
                                            <th style="width: 35px"></th>
                                        </tr>
                                        <tr [hidden]="showSimpleFilter">
                                            <th><d3s-column-filter [field]="'Name'" [datatype]="'text'"></d3s-column-filter></th>
                                            <th><d3s-column-filter [field]="'Type'" [datatype]="'text'"></d3s-column-filter></th>
                                            <th><d3s-column-filter [field]="'AllowFilter'" [datatype]="'text'"></d3s-column-filter></th>
                                            <th><d3s-column-filter [field]="'AllowSelect'" [datatype]="'text'"></d3s-column-filter></th>
                                            <th><d3s-column-filter [field]="'AllowSort'" [datatype]="'text'"></d3s-column-filter></th>
                                            <th></th>
                                            <th></th>
                                        </tr>
                                    </ng-template>
                                    <ng-template pTemplate="body" let-item>
                                        <tr (dblclick)="selected=item;showEditor=true" [pSelectableRow]="item">
                                            <td>{{item.Name}}</td>
                                            <td>{{item.Type}}</td>
                                            <td>
                                                <i *ngIf="item.AllowFilter" class="fa fa-check enabled" title="True"></i>
                                                <i *ngIf="!item.AllowFilter" class="fa fa-times disabled" title="False"></i>
                                            </td>
                                            <td>
                                                <i *ngIf="item.AllowSelect" class="fa fa-check enabled" title="True"></i>
                                                <i *ngIf="!item.AllowSelect" class="fa fa-times disabled" title="False"></i>   
                                            </td>
                                            <td>
                                                <i *ngIf="item.AllowSort" class="fa fa-check enabled" title="True"></i>
                                                <i *ngIf="!item.AllowSort" class="fa fa-times disabled" title="False"></i>  
                                            </td>
                                            <td>
                                                <div class="RowTools">
                                                    <a style="cursor:pointer;" (click)="selected=item;showEditor=true;"><i class="fa fa-pencil"></i></a>                                    
                                                </div>
                                            </td>
                                            <td>
                                                <div class="RowTools">
                                                    <a style="cursor:pointer;" (click)="selected=item;showDelete=true;"><i class="fa fa-trash-o"></i></a>                                    
                                                </div>
                                            </td>
                                        </tr>
                                    </ng-template>
                                    <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                                        <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                                    </ng-template>
                                </p-table>                                                           
                            </span>             
                            <d3s-admin-api-endpoint-version-fields-editor *ngIf="showEditor"
                                [model]="selected" 
                                [versionId]="version?.ID"
                                [entityId]="version?.EntityID"
                                (onCloseClick)="showEditor = false;"
                                (onSaveClick)="showEditor = false; load()">
                            </d3s-admin-api-endpoint-version-fields-editor>
                            <d3s-delete-form *ngIf="showDelete"
                                                        [callback]="theDeleteCallback"
                                                        [itemId]="selected?.ID"
                                                        method="callback"
                                                        [prompt]="'Are you sure you want to delete the  field [' + [selected?.Name] + ']?'"                                        
                                                        (onCancel)="showDelete=false;"
                                            ></d3s-delete-form>  
                    </div>                
                `
})

export class AdminCustomAPIEndpointVersionFieldsComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() version: ApiVersion;
    public showEditor: boolean = false;
    public showDelete: boolean = false;
    public fields: ApiField[] = [];
    public selected: ApiField = null;
    theDeleteCallback: Function;
            
    constructor(
        protected customAPIService: CustomAPIService,
        protected messagesService: MessagesService,
        private route: ActivatedRoute,
        private router: Router,
    ) {
        super();
        this.theDeleteCallback = this.deleteItem.bind(this);
    }

    ngOnInit(): void {
        this.load();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if ((changes['version'] && this.version != null)) {
            this.load();            
        }
    }

    private load(): void {
        this.isLoading = true;
        this.customAPIService.getEndpointVersionFields(this.version.ID).then(res => {
            this.fields = res;
            this.isLoading = false;
        });
    }

    private saveField(data): void {
        data.item.EntityID = this.version.EntityID;
        this.customAPIService.saveField(data.item).then(res => {
            this.showMessageForResult(this.messagesService, res);
            this.load();
            this.showEditor = false;
        })
    }   

    deleteItem(id: number) {
        this.customAPIService.deleteField(id).
            then(result => {
                this.showMessageForResult(this.messagesService, result);                
                this.showDelete = false;
                this.load();                
            });
    }
}