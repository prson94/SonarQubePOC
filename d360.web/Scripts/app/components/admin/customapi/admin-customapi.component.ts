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
                                <input #gb [hidden]="!showSimpleFilter" type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
                                <p-dataTable #dt sortField="Name" [sortOrder]="1" [globalFilter]="gb" [value]="services" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" (onRowDblclick)="selected=$event.data;showEditor=true" [(selection)]="selected" >                                                                        
                                    <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>                                    
                                    <p-column field="Name" header="Name" [sortable]="true" [filter]="!showSimpleFilter">
                                            <ng-template let-col let-item="rowData" pTemplate type="body">
	                                            <a (click)="showService(item);">{{item.Name}}</a>                                 
                                            </ng-template>
                                    </p-column>
                                    <p-column field="UriPrefix" header="Uri Segment" [sortable]="true" [filter]="!showSimpleFilter"></p-column>
                                    <p-column field="Description" header="Description" [sortable]="false" [filter]="!showSimpleFilter">
                                        <ng-template pTemplate type="body" let-item="rowData">
                                            <div [innerHtml]="item.Description"></div>
                                        </ng-template>
                                    </p-column>
                                    <p-column field="MaximumCacheAge" header="Cache Max-Age" [sortable]="false" [filter]="!showSimpleFilter"></p-column>
                                    <p-column [style]="{width:'40px'}">
                                        <ng-template let-service="rowData" pTemplate type="body">
                                            <div class="RowTools">
                                                <a style="cursor:pointer;" (click)="selected=service;showEditor=true"><i class="fa fa-pencil"></i></a>                                                                                        
                                            </div>
                                        </ng-template>
                                    </p-column> 
                                <p-column  [style]="{width:'40px'}" >
                                    <ng-template let-service="rowData" pTemplate type="body">
                                <div class="RowTools">                              
                                    <a  style="cursor:pointer;" (click)="selected=service;showDelete=true;"><i class="fa fa-trash-o"></i></a>                                    
                                </div>
                            </ng-template>
                        </p-column>      
                                </p-dataTable>                                  
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

    constructor(protected customAPIService: CustomAPIService, headerBreadcrumbService: HeaderBreadcrumbService, private messagesService: MessagesService, titleService: Title, private router: Router) {
        super(headerBreadcrumbService, titleService);
        this.areaName = "Custom API";
        this.setCommonItems();
        this.theDeleteCallback = this.deleteService.bind(this);
    }

    ngOnInit() : void {
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