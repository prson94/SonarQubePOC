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
                                <input #gb [hidden]="!showSimpleFilter" type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
                                <p-dataTable #dt sortField="Name" [sortOrder]="1" [globalFilter]="gb" [value]="versions" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" (onRowDblclick)="selected=$event.data;showEditor=true" [selection]="selected" (selectionChange)="selected=$event;selectedChange.emit(selected);" >                                                                        
                                    <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>                                    
                                    <p-column field="UriPrefix" header="Uri Segment" [sortable]="true" [filter]="!showSimpleFilter"></p-column>
                                    <p-column field="MajorVersion" header="Major Version" [sortable]="true" [filter]="!showSimpleFilter"></p-column>
                                    <p-column field="MinorVersion" header="Minor Version" [sortable]="true" [filter]="!showSimpleFilter"></p-column>
                                    <p-column [style]="{width:'40px'}">
                                        <ng-template let-service="rowData" pTemplate type="body">
                                            <div class="RowTools">
                                                <a style="cursor:pointer;" (click)="selected=service;showEditor=true"><i class="fa fa-pencil"></i></a>                                                                                        
                                            </div>
                                        </ng-template>
                                    </p-column>                                                                                    
                                </p-dataTable>                                  
                            </span>             
                            <d3s-dynamic-editor *ngIf="showEditor" [parentID]="endpoint?.ID" [objectID]="selected?.ID" [objectType]="'Version'" [title]="'Version'" [selection]="selected" (saveClick)="saveVersion($event)" (closeClick)="showEditor=false"></d3s-dynamic-editor>
                    </div>
                
                `
})

export class AdminCustomAPIEndpointVersionsComponent extends BaseComponent implements OnInit {
    @Input() endpoint: ApiEndpoint;
    public showEditor: boolean = false;
    public versions: ApiVersion[] = [];
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
}