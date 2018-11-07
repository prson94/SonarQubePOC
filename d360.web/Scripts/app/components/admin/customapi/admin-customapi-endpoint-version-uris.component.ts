import { Component, OnInit, Input, Output, EventEmitter, OnChanges, SimpleChange } from '@angular/core';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SurveysService } from '../../../services/surveys.service';
import { MessagesService } from '../../../services/messages.service';
import { BaseComponent } from '../../shared/base.component';
import { CustomAPIService } from '../../../services/custom-api.service';
import { ApiService, ApiEndpoint, ApiVersion, ApiUri } from '../../../models/custom-api.model';
import { Router, ActivatedRoute } from '@angular/router';

@Component({
    selector: 'd3s-admin-api-endpoint-version-uritypes',
    providers: [CustomAPIService],
    template: `                                 
                        <div class="tile tile-detail">
                            <header *ngIf="!showEditor && !showDelete">URI Types for v{{version?.MajorVersion}}.{{version?.MinorVersion}}
                            <d3s-tile-actions [hasAdd]="true" (addClick)="selected=null;showEditor=true;" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
                            </header>
                            <d3s-loading [isLoading]="isLoading"></d3s-loading>
                            <span *ngIf="!isLoading && !showDelete && !showEditor">
                                
                                <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..." class="grid-simple-filter">
                                <p-table #dt sortField="Name" [sortOrder]="1" [value]="uris" selectionMode="single" [globalFilterFields]="['Format','UriType']" [pageLinks]="3" [paginator]="true" [rows]="10" [(selection)]="selected">
                                    <ng-template pTemplate="header">
                                        <tr>
                                            <th [pSortableColumn]="'Format'">
                                                Segment
                                                <d3s-sortIcon [field]="'Format'"></d3s-sortIcon>
                                            </th>
                                            <th [pSortableColumn]="'UriType'">
                                                Type
                                                <d3s-sortIcon [field]="'UriType'"></d3s-sortIcon>
                                            </th>
                                            <th style="width:40px"></th>
                                            <th style="width:40px"></th>
                                        </tr>
                                        <tr [hidden]="showSimpleFilter">
                                            <th><d3s-column-filter [field]="'Format'" [datatype]="'text'"></d3s-column-filter></th>
                                            <th><d3s-column-filter [field]="'UriType'" [datatype]="'text'"></d3s-column-filter></th>
                                            <th></th>
                                            <th></th>
                                        </tr>
                                    </ng-template>
                                    <ng-template pTemplate="body" let-item>
                                        <tr (dblclick)="selected=item;showEditor=true" [pSelectableRow]="item">
                                            <td>{{item.Format}}</td>
                                            <td>
                                                <span *ngIf="item.UriType == 1">Collection</span>
                                                <span *ngIf="item.UriType == 2">Singleton</span>   
                                            </td>
                                            <td>
                                                <div class="RowTools">
                                                    <a style="cursor:pointer;" (click)="selected=item;showEditor=true"><i class="fa fa-pencil"></i></a>                                                                                        
                                                </div>
                                            </td>
                                            <td>
                                                <div class="RowTools">                              
                                                    <a  style="cursor:pointer;" (click)="selected=item;showDelete=true;"><i class="fa fa-trash-o"></i></a>                                    
                                                </div>
                                            </td>
                                        </tr>
                                    </ng-template>
                                    <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                                        <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                                    </ng-template>
                                </p-table>
                            </span>             
                            <d3s-dynamic-editor *ngIf="showEditor" [parentID]="version?.ID" [objectID]="selected?.ID" [objectType]="'Uri'" [title]="'Uri'" [selection]="selected" (saveClick)="saveUri($event)" (closeClick)="showEditor=false"></d3s-dynamic-editor>
                             <d3s-delete-form *ngIf="showDelete"
                                [callback]="theDeleteCallback"
                                [itemId]="selected?.ID"
                                [method]="'callback'"
                                [prompt]="'Are you sure you want to delete the uri [' + [selected?.Format] + ']?'"                                         
                                (onCancel)="showDelete=false;">
                            </d3s-delete-form>
                    </div>                
                `
})

export class AdminCustomAPIEndpointVersionUriTypesComponent extends BaseComponent implements OnInit {
    @Input() version: ApiVersion;
    public showEditor: boolean = false;
    public uris: ApiUri[] = [];
    public selected: ApiUri = null;
    public showDelete: boolean = false;
    theDeleteCallback: Function;
    
    constructor(
        protected customAPIService: CustomAPIService,
        protected messagesService: MessagesService,
        private route: ActivatedRoute,
        private router: Router,
    ) {
        super();
        this.theDeleteCallback = this.deleteService.bind(this);
    }

    ngOnInit(): void {
        this.load();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if ((changes['version'] || this.version != null)) {
            this.load();            
        }
    }

    private load(): void {
        this.isLoading = true;
        this.customAPIService.getEndpointVersionUris(this.version.ID).then(res => {
            this.uris = res;
            this.isLoading = false;
        });
    }

    private saveUri(data): void {
        this.customAPIService.saveEndpointUri(data.item).then(res => {
            this.showMessageForResult(this.messagesService, res);
            this.load();
            this.showEditor = false;
        })
    }

    deleteService(id: number) {
        this.customAPIService.deleteEndpointUri(id).
            then(result => {
                this.showMessageForResult(this.messagesService, result);
                this.showDelete = false;
                this.load();

            });
    }
}