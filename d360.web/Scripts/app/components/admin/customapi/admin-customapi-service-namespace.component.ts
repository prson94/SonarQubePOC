import { Component, OnInit, OnDestroy, Input, SimpleChange } from '@angular/core';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SurveysService } from '../../../services/surveys.service';
import { MessagesService } from '../../../services/messages.service';
import { AdminBaseComponent } from '../admin-base.component';
import { Title } from '@angular/platform-browser';
import { CustomAPIService } from '../../../services/custom-api.service';
import { ApiService, ApiNamespace } from '../../../models/custom-api.model';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';
import { RightSidebarItem } from '../../../models/rightsidebar.model';
import { RightSidebarService } from '../../../services/right-sidebar.service';

@Component({
    selector: 'd3s-admin-customapi-service-namespace',
    providers: [CustomAPIService],
    template: ` 
                        <div class="tile tile-detail">
                            <header *ngIf="!showEditor && !showDelete && !showDelete">Namespaces
                            <d3s-tile-actions [hasAdd]="true" (addClick)="selected=null;showEditor=true;" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
                            </header>
                            <d3s-loading [isLoading]="isLoading"></d3s-loading>
                            <span *ngIf="!isLoading && !showDelete && !showEditor">
                                <input #gb [hidden]="!showSimpleFilter" type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
                                <p-dataTable #dt sortField="Name" [sortOrder]="1" [globalFilter]="gb" [value]="fields" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" (onRowDblclick)="selected=$event.data;showEditor=true" [(selection)]="selected">                                                                        
                                    <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>                                    
                                    <p-column field="Node" header="Element Name" [sortable]="true" [filter]="!showSimpleFilter"></p-column>
                                    <p-column field="Namespace" header="Namespace" [sortable]="true" [filter]="!showSimpleFilter"></p-column>                                    
                                    <p-column  [style]="{width:'35px'}">
                                            <ng-template let-item="rowData" pTemplate type="body">
                                                <div class="RowTools">
                                                    <a style="cursor:pointer;" (click)="selected=item;showEditor=true;"><i class="fa fa-pencil"></i></a>                                    
                                                </div>
                                            </ng-template>
                                    </p-column> 
                                     <p-column [style]="{width:'35px'}">
                                            <ng-template let-item="rowData" pTemplate type="body">
                                                <div class="RowTools">
                                                    <a style="cursor:pointer;" (click)="selected=item;showDelete=true;"><i class="fa fa-trash-o"></i></a>                                    
                                                </div>
                                            </ng-template>
                                    </p-column> 
                                </p-dataTable>                                  
                            </span>             
                            <d3s-dynamic-editor *ngIf="showEditor" [parentID]="serviceId" [objectID]="selected?.ID" [objectType]="'Namespace'" [title]="'Namespace'" [selection]="selected" (saveClick)="saveField($event)" (closeClick)="showEditor=false"></d3s-dynamic-editor>
                            <d3s-delete-form *ngIf="showDelete"
                                                        [callback]="theDeleteCallback"
                                                        [itemId]="selected?.ID"
                                                        method="callback"
                                                        [prompt]="'Are you sure you want to delete the  namespace [' + [selected?.Node] + ']?'"                                        
                                                        (onCancel)="showDelete=false;"
                                            ></d3s-delete-form>  
                    </div>  
                `
})

export class AdminCustomAPIServiceNamespaceComponent extends AdminBaseComponent implements OnInit {
    serviceId: number;
    public showEditor: boolean = false;
    public showDelete: boolean = false;
    public fields: ApiNamespace[] = [];
    public selected: ApiNamespace = null;
    theDeleteCallback: Function;
    private sub: any;
    public service: ApiService = null;


    constructor(
        private route: ActivatedRoute,
        private router: Router,
        protected customAPIService: CustomAPIService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        rightSidebarService: RightSidebarService,
        private messagesService: MessagesService,
        titleService: Title
    ) {
        super(headerBreadcrumbService, titleService, rightSidebarService);
        this.areaName = "Custom API";
        this.theDeleteCallback = this.deleteItem.bind(this);
    }

    ngOnInit(): void {
        this.sub = this.route.params.subscribe(params => {
            this.serviceId = +params['serviceId']; // (+) converts string 'id' to a number            
            this.isLoading = true;
            this.customAPIService.getService(this.serviceId).then(res => {
                this.service = res;
                this.load();
                this.headerBreadcrumbService.clearBreadcrumbs();
                this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Administration'));
                this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Custom API', '/admin/customapi'));
                this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(`${this.service.Name}`));
            });
        });
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes['serviceId']) {
            this.load();
        }
    }

    private load(): void {
        this.isLoading = true;
        this.customAPIService.getNamespaces(this.serviceId).then(res => {
            this.fields = res;
            this.isLoading = false;
        });
    }

    private saveField(data): void {
        this.customAPIService.saveNamespace(data.item).then(res => {
            this.showMessageForResult(this.messagesService, res);
            this.load();
            this.showEditor = false;
        })
    }

    deleteItem(id: number) {
        this.customAPIService.deleteNamespace(id).
            then(result => {
                this.showMessageForResult(this.messagesService, result);
                this.showDelete = false;
                this.load();
            });
    }
}