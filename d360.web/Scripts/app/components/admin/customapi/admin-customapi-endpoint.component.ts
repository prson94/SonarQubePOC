import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SurveysService } from '../../../services/surveys.service';
import { MessagesService } from '../../../services/messages.service';
import { BaseComponent } from '../../shared/base.component';
import { CustomAPIService } from '../../../services/custom-api.service';
import { ApiService, ApiEndpoint } from '../../../models/custom-api.model';

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
                                <input #gb [hidden]="!showSimpleFilter" type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
                                <p-dataTable #dt sortField="Name" [sortOrder]="1" [globalFilter]="gb" [value]="endpoints" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" (onRowDblclick)="selected=$event.data;showEditor=true" [(selection)]="selected" >                                                                        
                                    <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>
                                    <p-column field="Name" header="Name" [sortable]="true" [filter]="!showSimpleFilter"></p-column>                                                                                        
                                    <p-column field="UriPrefix" header="Uri Segment" [sortable]="true" [filter]="!showSimpleFilter"></p-column>
                                    <p-column field="Description" header="Description" [sortable]="false" [filter]="!showSimpleFilter">
                                        <ng-template pTemplate type="body" let-item="rowData">
                                            <div [innerHtml]="item.Description"></div>
                                        </ng-template>
                                    </p-column>
                                    <p-column [style]="{width:'40px'}">
                                        <ng-template let-service="rowData" pTemplate type="body">
                                            <div class="RowTools">
                                                <a style="cursor:pointer;" (click)="selected=service;showEditor=true"><i class="fa fa-pencil"></i></a>                                                                                        
                                            </div>
                                        </ng-template>
                                    </p-column>                                                                                    
                                </p-dataTable>                                  
                            </span>             
                            <d3s-dynamic-editor *ngIf="showEditor" [parentID]="service?.ID" [objectID]="selected?.ID" [objectType]="'Endpoint'" [title]="'Endpoint'" [selection]="selected" (saveClick)="saveEndpoint($event)" (closeClick)="showEditor=false"></d3s-dynamic-editor>
                    </div>
                
                `
})

export class AdminCustomAPIEndpointsComponent extends BaseComponent implements OnInit {
    @Input() service: ApiService;    
    public showEditor: boolean = false;
    public endpoints: ApiEndpoint[] = [];
    public selected: ApiEndpoint = null;

    @Input() numberOfEndpoints: number = 0;
    @Output() numberOfEndpointsChange = new EventEmitter();

    constructor(protected customAPIService: CustomAPIService, protected messagesService: MessagesService) {
        super();                
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
}