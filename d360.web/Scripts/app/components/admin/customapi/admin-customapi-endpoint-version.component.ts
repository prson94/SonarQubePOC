import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SurveysService } from '../../../services/surveys.service';
import { MessagesService } from '../../../services/messages.service';
import { BaseComponent } from '../../shared/base.component';
import { CustomAPIService } from '../../../services/custom-api.service';
import { ApiService, ApiEndpoint, ApiVersion } from '../../../models/custom-api.model';
import { Router, ActivatedRoute } from '@angular/router';

@Component({
    selector: 'd3s-admin-api-endpoint-versions',
    providers: [CustomAPIService],
    template: `                                 
                        <div class="tile tile-detail">
                            <header *ngIf="!showEditor && !showDelete">Versions
                            <d3s-tile-actions [hasAdd]="true" (addClick)="selected=null;showEditor=true;" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
                            </header>
                            <d3s-loading [isLoading]="isLoading"></d3s-loading>
                            <span *ngIf="!isLoading && !showDelete && !showEditor">
                                <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..." class="grid-simple-filter">
                                <p-table #dt [value]="versions" selectionMode="single" [globalFilterFields]="['UriPrefix','MajorVersion','MinorVersion']" [pageLinks]="3" [paginator]="true" [rows]="10">
                                    <ng-template pTemplate="header">
                                        <tr>
                                            <th [pSortableColumn]="'UriPrefix'">
                                                Uri Segment
                                                <d3s-sortIcon [field]="'UriPrefix'"></d3s-sortIcon>
                                            </th>
                                            <th [pSortableColumn]="'MajorVersion'">
                                                Major Version
                                                <d3s-sortIcon [field]="'MajorVersion'"></d3s-sortIcon>
                                            </th>
                                            <th [pSortableColumn]="'MinorVersion'">
                                                Minor Version
                                                <d3s-sortIcon [field]="'MinorVersion'"></d3s-sortIcon>
                                            </th>
                                            <th style="width: 40px"></th>
                                            <th style="width: 40px"></th>
                                        </tr>
                                        <tr [hidden]="showSimpleFilter">
                                            <th><d3s-column-filter [field]="'UriPrefix'" [datatype]="'text'"></d3s-column-filter></th>
                                            <th><d3s-column-filter [field]="'MajorVersion'" [datatype]="'text'"></d3s-column-filter></th>
                                            <th><d3s-column-filter [field]="'MinorVersion'" [datatype]="'text'"></d3s-column-filter></th>
                                            <th></th>
                                            <th></th>
                                        </tr>
                                    </ng-template>
                                    <ng-template pTemplate="body" let-item>
                                        <tr (dblclick)="selected=item;showEditor=true" [pSelectableRow]="item">
                                            <td>{{item.UriPrefix}}</td>
                                            <td>{{item.MajorVersion}}</td>
                                            <td>{{item.MinorVersion}}</td>
                                            <td>
                                                <div class="RowTools">
                                                    <a style="cursor:pointer;" (click)="selected=item;showEditor=true"><i class="fa fa-pencil"></i></a>
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
                            <d3s-dynamic-editor *ngIf="showEditor" [parentID]="endpoint?.ID" [objectID]="selected?.ID" [objectType]="'Version'" [title]="'Version'" [selection]="selected" (saveClick)="saveVersion($event)" (closeClick)="showEditor=false"></d3s-dynamic-editor>
                            <d3s-delete-form *ngIf="showDelete"
                                [callback]="theDeleteCallback"
                                [itemId]="selected?.ID"
                                [method]="'callback'"
                                [prompt]="'Are you sure you want to delete the version [' + [selected?.UriPrefix] + ']?'"                                         
                                (onCancel)="showDelete=false;">
                            </d3s-delete-form>
                    </div>
                
                `
})

export class AdminCustomAPIEndpointVersionsComponent extends BaseComponent implements OnInit {
    @Input() endpoint: ApiEndpoint;
    public showEditor: boolean = false;
    public versions: ApiVersion[] = [];
    public showDelete: boolean = false;
    theDeleteCallback: Function;
    @Input() selected: ApiVersion = null;
    @Output() selectedChange = new EventEmitter();

    @Input() numberOfVersions: number = 0;
    @Output() numberOfVersionsChange = new EventEmitter();
        
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

    private load(): void {
        this.isLoading = true;
        this.customAPIService.getEndpointVersions(this.endpoint.ID).then(res => {
            this.versions = res;
            if (this.versions && this.versions.length > 0) {
                this.selected = this.versions[0];
                this.selectedChange.emit(this.selected);
            }
            this.numberOfVersions = (res != null && res.length > 0) ? res.length : 0;
            this.numberOfVersionsChange.emit(this.numberOfVersions);
            this.isLoading = false;
        });
    }

    private saveVersion(data): void {
        this.customAPIService.saveVersion(data.item).then(res => {
            this.showMessageForResult(this.messagesService, res);
            this.load();
            this.showEditor = false;
        })
    }  

    deleteService(id: number) {
        this.customAPIService.deleteEndpointVersion(id).
            then(result => {
                this.showMessageForResult(this.messagesService, result);
                this.showDelete = false;
                this.load();

            });
    }
}