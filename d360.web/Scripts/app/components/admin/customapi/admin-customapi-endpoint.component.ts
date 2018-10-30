import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SurveysService } from '../../../services/surveys.service';
import { MessagesService } from '../../../services/messages.service';
import { BaseComponent } from '../../shared/base.component';
import { CustomAPIService } from '../../../services/custom-api.service';
import { ApiService, ApiEndpoint } from '../../../models/custom-api.model';
import { Router, ActivatedRoute } from '@angular/router';

@Component({
    selector: 'd3s-admin-api-endpoints',
    providers: [CustomAPIService],
    template: `                                 
                        <div class="tile tile-detail">
                            <header *ngIf="!showEditor && !showDelete">Endpoints
                            <d3s-tile-actions [hasAdd]="true" (addClick)="selected=null;showEditor=true;" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
                            </header>
                            <d3s-loading [isLoading]="isLoading"></d3s-loading>
                            <span *ngIf="!isLoading && !showDelete && !showEditor">
                                <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..." class="grid-simple-filter">
                                <p-table #dt [value]="endpoints" selectionMode="single" [globalFilterFields]="['Name','UriPrefix','Description']" [pageLinks]="3" [paginator]="true" [rows]="10" [(selection)]="selected">
                                    <ng-template pTemplate="header">
                                        <tr>
                                            <th [pSortableColumn]="'Name'">
                                                Name
                                                <d3s-sortIcon [field]="'Name'"></d3s-sortIcon>
                                            </th>
                                            <th [pSortableColumn]="'UriPrefix'">
                                                Uri Segment
                                                <d3s-sortIcon [field]="'UriPrefix'"></d3s-sortIcon>
                                            </th>
                                            <th>Description</th>
                                            <th style="width: 40px"></th>
                                            <th style="width: 40px"></th>
                                        </tr>
                                        <tr [hidden]="showSimpleFilter">
                                            <th><d3s-column-filter [field]="'Name'" [datatype]="'text'"></d3s-column-filter></th>
                                            <th><d3s-column-filter [field]="'UriPrefix'" [datatype]="'text'"></d3s-column-filter></th>
                                            <th><d3s-column-filter [field]="'Description'" [datatype]="'text'"></d3s-column-filter></th>
                                            <th></th>
                                            <th></th>
                                        </tr>
                                    </ng-template>
                                    <ng-template pTemplate="body" let-item>
                                        <tr (dblclick)="selected=item;showEditor=true" [pSelectableRow]="item">
                                            <td>
                                                <a (click)="showEndpoint(item);">{{item.Name}}</a>
                                            </td>
                                            <td>{{item.UriPrefix}}</td>
                                            <td>
                                                <div [innerHtml]="item.Description"></div>
                                            </td>
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
                            <d3s-dynamic-editor *ngIf="showEditor" [parentID]="service?.ID" [objectID]="selected?.ID" [objectType]="'Endpoint'" [title]="'Endpoint'" [selection]="selected" (saveClick)="saveEndpoint($event)" (closeClick)="showEditor=false"></d3s-dynamic-editor>
                            <d3s-delete-form *ngIf="showDelete"
                            [callback]="theDeleteCallback"
                            [itemId]="selected?.ID"
                            [method]="'callback'"
                            [prompt]="'Are you sure you want to delete the end point [' + [selected?.Name] + ']?'"                                         
                            (onCancel)="showDelete=false;"
                        ></d3s-delete-form>
                    </div>
                
                `
})

export class AdminCustomAPIEndpointsComponent extends BaseComponent implements OnInit {
    @Input() service: ApiService;    
    public showEditor: boolean = false;
    public endpoints: ApiEndpoint[] = [];
    public selected: ApiEndpoint = null;
    public showDelete: boolean = false;

    theDeleteCallback: Function;

    @Input() numberOfEndpoints: number = 0;
    @Output() numberOfEndpointsChange = new EventEmitter();

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
        this.customAPIService.getEndpoints(this.service.ID).then(res => {
            this.isLoading = false;
            this.endpoints = res;
            this.numberOfEndpoints = this.endpoints.length;
            this.numberOfEndpointsChange.emit(this.numberOfEndpoints);
        });
    }

    private saveEndpoint(data): void {
        this.customAPIService.saveEndpoint(data.item).then(res => {
            this.showMessageForResult(this.messagesService, res);
            this.load();
            this.showEditor = false;
        })   
    }

    public showEndpoint(item: ApiEndpoint): void {
        this.router.navigateByUrl(`admin/customapi/${this.service.ID}/details/${item.ID}/details`);
    }

    deleteService(id: number) {
        this.customAPIService.deleteEndpoint(id).
            then(result => {
                this.showMessageForResult(this.messagesService, result);
                this.showDelete = false;
                this.load();

            });
    }
}