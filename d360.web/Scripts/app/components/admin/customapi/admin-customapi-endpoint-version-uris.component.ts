import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
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
                                <input #gb [hidden]="!showSimpleFilter" type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
                                <p-dataTable #dt sortField="Name" [sortOrder]="1" [globalFilter]="gb" [value]="uris" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" (onRowDblclick)="selected=$event.data;showEditor=true" [(selection)]="selected">                                                                        
                                    <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>                                    
                                    <p-column field="Format" header="Segment" [sortable]="true" [filter]="!showSimpleFilter"></p-column>
                                    <p-column field="UriType" header="Type" [sortable]="true" [filter]="!showSimpleFilter">
                                        <ng-template let-row="rowData" pTemplate type="body">
                                            <span *ngIf="row.UriType == 1">Collection</span>
                                            <span *ngIf="row.UriType == 2">Singleton</span>                                            
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
                            <d3s-dynamic-editor *ngIf="showEditor" [parentID]="version?.ID" [objectID]="selected?.ID" [objectType]="'Uri'" [title]="'Uri'" [selection]="selected" (saveClick)="saveUri($event)" (closeClick)="showEditor=false"></d3s-dynamic-editor>
                    </div>                
                `
})

export class AdminCustomAPIEndpointVersionUriTypesComponent extends BaseComponent implements OnInit {
    @Input() version: ApiVersion;
    public showEditor: boolean = false;
    public uris: ApiUri[] = [];
    public selected: ApiUri = null;
    
    constructor(
        protected customAPIService: CustomAPIService,
        protected messagesService: MessagesService,
        private route: ActivatedRoute,
        private router: Router,
    ) {
        super();
    }

    ngOnInit(): void {
        this.load();
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
}