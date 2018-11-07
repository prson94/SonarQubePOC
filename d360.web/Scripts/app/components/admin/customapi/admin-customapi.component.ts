import { Component, OnInit } from '@angular/core';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SurveysService } from '../../../services/surveys.service';
import { MessagesService } from '../../../services/messages.service';
import { AdminBaseComponent } from '../admin-base.component';
import { Title } from '@angular/platform-browser';
import { CustomAPIService } from '../../../services/custom-api.service';
import { ApiService } from '../../../models/custom-api.model';
import { Router } from '@angular/router';
import { RightSidebarService } from '../../../services/right-sidebar.service';

@Component({
    selector: 'd3s-admin-customapi',
    providers: [CustomAPIService],
    template: ` 
                <div class="row">
                    <div class="col s12">                    
                        <div class="tile tile-detail">
                            <header *ngIf="!showEditor && !showDelete">Services
                            <d3s-tile-actions [hasAdd]="true" (addClick)="selected=null;showEditor=true;" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
                            </header>
                            <d3s-loading [isLoading]="isLoading"></d3s-loading>
                            <span *ngIf="!isLoading && !showDelete && !showEditor">
                                <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..." class="grid-simple-filter">
                                <p-table #dt [value]="services" selectionMode="single" [globalFilterFields]="['Name','UriPrefix','Description','MaximumCacheAge']" sortField="Name" [sortOrder]="1" [pageLinks]="3" [paginator]="true" [rows]="10" [(selection)]="selected">
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
                                            <th>Cache Max-Age</th>
                                            <th style="width: 40px"></th>
                                            <th style="width: 40px"></th>
                                        </tr>
                                        <tr [hidden]="showSimpleFilter">
                                            <th><d3s-column-filter [field]="'Name'" [datatype]="'text'"></d3s-column-filter></th>
                                            <th><d3s-column-filter [field]="'UriPrefix'" [datatype]="'text'"></d3s-column-filter></th>
                                            <th><d3s-column-filter [field]="'Description'" [datatype]="'text'"></d3s-column-filter></th>
                                            <th><d3s-column-filter [field]="'MaximumCacheAge'" [datatype]="'text'"></d3s-column-filter></th>
                                            <th></th>
                                            <th></th>
                                        </tr>
                                    </ng-template>
                                    <ng-template pTemplate="body" let-item>
                                        <tr (dblclick)="selected=item;showEditor=true" [pSelectableRow]="item">
                                            <td>
                                                <a (click)="showService(item);">{{item.Name}}</a>
                                            </td>
                                            <td>{{item.UriPrefix}}</td>
                                            <td>
                                                <div [innerHtml]="item.Description"></div>
                                            </td>
                                            <td>{{item.MaximumCacheAge}}</td>
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
                            <d3s-dynamic-editor *ngIf="showEditor" [objectID]="selected?.ID" [objectType]="'Service'" [title]="'APIService'" [selection]="selected" (saveClick)="saveService($event)" (closeClick)="showEditor=false"></d3s-dynamic-editor>
                           <d3s-delete-form *ngIf="showDelete"
                            [callback]="theDeleteCallback"
                            [itemId]="selected?.ID"
                            [method]="'callback'"
                            [prompt]="'Are you sure you want to delete the api service [' + [selected?.Name] + ']?'"                                         
                            (onCancel)="showDelete=false;"
                        ></d3s-delete-form>
                        </div>
                    </div>
                </div>  
                `
})

export class AdminCustomAPIComponent extends AdminBaseComponent implements OnInit {

    public selected: ApiService = null;
    public services: ApiService[] = [];
    public showEditor: boolean = false;
    public showDelete: boolean = false;

    theDeleteCallback: Function;

    constructor(protected customAPIService: CustomAPIService, rightSidebarService: RightSidebarService, headerBreadcrumbService: HeaderBreadcrumbService, private messagesService: MessagesService, titleService: Title, private router: Router) {
        super(headerBreadcrumbService, titleService, rightSidebarService);
        this.areaName = "Custom API";
        this.setCommonItems();
        this.clearSidebar();
        this.theDeleteCallback = this.deleteService.bind(this);
    }

    ngOnInit(): void {
        this.rightSidebarService.clearItems();
        this.load();
    }

    private load() : void {
        this.isLoading = true;
        this.customAPIService.getServices().then(res => {
            this.services = res;
            this.isLoading = false;
        });
    }

    public saveService(data): void {
        this.showEditor = false;                
        this.customAPIService.saveService(data.item).then(res => {
            this.showMessageForResult(this.messagesService, res);
            this.load();            
        })
    }

    public showService(item: ApiService): void {
        this.router.navigateByUrl(`admin/customapi/${item.ID}/details`);
    }

    deleteService(id: number) {
        this.customAPIService.deleteService(id).
            then(result => {
                this.showMessageForResult(this.messagesService, result);
                this.showDelete = false;
                this.load();
          
            });
    }
}