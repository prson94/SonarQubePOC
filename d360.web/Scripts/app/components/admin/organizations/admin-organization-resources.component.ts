import { Component, Input, OnChanges, SimpleChange } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { Organization, OrganizationResource } from '../../../models/organization.model';
import { OrganizationsService } from '../../../services/organizations.service';
import { MessagesService } from '../../../services/messages.service';
import { BaseComponent } from '../../shared/base.component';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';

@Component({
    selector: 'd3s-admin-organization-resources',
    providers: [OrganizationsService],
    template: `
               <header>Users for this organization 
                <d3s-tile-actions [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
               </header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <span *ngIf="!isLoading">
                    <input [hidden]="!showSimpleFilter" #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
                    <p-dataTable #dt [globalFilter]="gb" [value]="resources" selectionMode="single" [rows]="defaultInitialItemsPerPage" paginator="true" pageLinks="3" [rowsPerPageOptions]="defaultPagingOptions" (onRowDblclick)="selected=$event.data;showEditor=true" [(selection)]="selected" >                                                                        
                        <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>
                        <p-column header="Name" [style]="{width:'140px'}">
                            <ng-template let-item="rowData" pTemplate type="body">
                                <a (click)="openResource(item)">{{item.FirstName}} {{item.LastName}}</a>
                            </ng-template>
                        </p-column>
                        <p-column field="Email" header="Email" [sortable]="true" [filter]="!showSimpleFilter"></p-column>
                        <p-column field="Status" header="Status" [sortable]="true" [filter]="!showSimpleFilter" [style]="{width:'100px'}"></p-column>
                        <p-column field="Accepted" header="Accepted" [sortable]="true" [filter]="!showSimpleFilter" [style]="{width:'100px'}"></p-column>
                        <p-column field="DateAccepted" header="Accepted On" [sortable]="true" [style]="{width:'150px'}">
                            <ng-template let-col let-item="rowData" pTemplate type="body">
                                <span>{{item.DateAccepted | date : 'short'}}</span>
                            </ng-template>
                        </p-column>
                        <p-column field="DateLastLoggedIn" header="Last Logon" [sortable]="true" [style]="{width:'120px'}">
                            <ng-template let-col let-item="rowData" pTemplate type="body">
                                <span>{{item.DateLastLoggedIn | date : 'short'}}</span>
                            </ng-template>
                        </p-column>
                    </p-dataTable>  
                </span>

                `
})

export class AdminOrganizationResourcesComponent extends BaseComponent implements OnChanges {
    @Input() organization: Organization = null;

    error: any;
    isLoading: boolean = false;

    resources: OrganizationResource[] = [];
    selected: OrganizationResource;

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private organizationsService: OrganizationsService,
        private messagesService: MessagesService) {
        super();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.organization != null) this.getResources();
    }

    getResources() {
        this.isLoading = true;
        this.organizationsService
            .getUsersByOrganization(this.organization.ID)
            .then(result => {
                this.resources = result;
                this.selected = (this.resources.length > 0 ? this.resources[0] : null);                
                this.isLoading = false;
            })
            .catch(error => this.error = error);
    }

    private openResource(event) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('resource', event.ResourceID));
    }
}


