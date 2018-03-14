import { Component, Input, OnChanges, SimpleChange, Output, EventEmitter } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { OrganizationsService } from '../../../services/organizations.service';
import { MessagesService } from '../../../services/messages.service';
import { BaseComponent } from '../../shared/base.component';
import { Organization, OrganizationType } from '../../../models/organization.model';

@Component({
    selector: 'd3s-admin-organization-list-component',
    providers: [OrganizationsService],
    template: `        
    <div class="tile tile-detail">
        <header *ngIf="!showEditor && !showDelete && !showHistory">
            Organizations
            <d3s-tile-actions [hasAdd]="true" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter" (addClick)="add()"></d3s-tile-actions>
        </header>
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div *ngIf="!isLoading && !showEditor && !showDelete && !showHistory">
            <input #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
            <p-dataTable #dt sortField="Name" [sortOrder]="1" [globalFilter]="gb" [value]="organizations" selectionMode="single" [rows]="20" [paginator]="true" [pageLinks]="3" expandableRows="true" [selection]="organization" (selectionChange)="organization=$event;organizationChange.emit($event);" (onRowSelect)="organization=$event.data;organizationChange.emit(organization);" (onRowDblclick)="organization=$event.data;showEditor=true;organizationChange.emit(organization);">
                <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>
                <p-column field="Name" header="Name" sortable="true"  [filter]="!showSimpleFilter"></p-column>
                <p-column field="AdministratorEmail" header="Administrator Email"></p-column>
                <p-column field="DateAccepted" header="Accepted On">
                    <ng-template pTemplate type="body" let-item="rowData">
                        {{ item.DateAccepted | date: 'short' }}
                    </ng-template>
                </p-column>
                <p-column field="AcceptedBy" header="Accepted By">
                    <ng-template let-item="rowData" pTemplate type="body">
                        <a (click)="openResource(item)">{{item.AcceptedByName}}</a>
                    </ng-template>
                </p-column>
                <p-column [style]="{width:'40px'}">
                    <ng-template let-item="rowData"  pTemplate type="body">
                        <div class="RowTools">
                            <a style="cursor:pointer;" (click)="organization=item;showEditor=true"><i class="fa fa-pencil"></i></a>
                        </div>
                    </ng-template>
                </p-column>
                <p-column  [style]="{width:'40px'}">
                    <ng-template let-item="rowData" pTemplate type="body">
                        <div class="RowTools">
                            <a style="cursor:pointer;" (click)="organization=item;showDelete=true"><i class="fa fa-trash-o"></i></a>
                        </div>
                    </ng-template>
                </p-column>
                <p-column [style]="{width:'40px'}">
                    <ng-template let-item="rowData" pTemplate type="body">
                        <div class="RowTools">
                            <a style="cursor:pointer;" (click)="organization=item;showHistory=true"><i class="fa fa-history"></i></a>                                        
                        </div>
                    </ng-template>
                </p-column>
            </p-dataTable>
        </div>
        <d3s-dynamic-editor *ngIf="showEditor" [objectID]="organizationType?.ID" [objectType]="'Organization'" [title]="'Organization'" [selection]="organization" (saveClick)="save($event)" (closeClick)="closeEditor()"></d3s-dynamic-editor>     
        <d3s-delete-form *ngIf="showDelete"
            [callback]="theDeleteCallback"
            [itemId]="organization?.ID"
            [method]="'callback'"
            [prompt]="'Are you sure you want to delete the organization [' + [organization?.Name] + ']?'"                                         
            (onCancel)="showDelete=false;"
        ></d3s-delete-form>
        <div *ngIf="showHistory">
            <d3s-admin-organization-contract-history type="organization" [id]="organization?.ID" [objectName]="(organization?.Name || 'Organization')" (onClose)="showHistory = false"></d3s-admin-organization-contract-history>
        </div>
    </div>
    `
})

export class AdminOrganizationListComponent extends BaseComponent implements OnChanges {
    @Input() organizationType: OrganizationType = null;
    @Input() organization: Organization;
    @Output() organizationChange = new EventEmitter();

    organizations: Organization[] = [];
    showEditor: boolean = false;
    showDelete: boolean = false;
    showHistory: boolean = false;
    theDeleteCallback: Function;

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private organizationService: OrganizationsService,
        private messagesService: MessagesService) {
        super();

        this.theDeleteCallback = this.deleteOrganization.bind(this);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {     
        if (changes['organizationType']) {
            if (this.organizationType != null) {
                this.getOrganizations();
            } else {
                this.organizations = [];
                if (this.organization != null) {
                    this.organization = null;
                    this.organizationChange.emit(null);
                }
            }
        }
    }

    getOrganizations() {
        this.isLoading = true;
        this.organizations = [];
        this.organizationService.getOrganizationsByType(this.organizationType.ID)
            .then(result => {
                this.organizations = result;
                this.isLoading = false;
                if (this.organizations.length > 0)
                    this.organization = this.organizations[0];
                else
                    this.organization = null;

                this.organizationChange.emit(this.organization);
            });
    }
        
    deleteOrganization(id: number) {
        this.organizationService.deleteOrganization(id)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                this.showDelete = false;
                if (result.type != 'error') {
                    this.organization = this.organizations.length > 0 ? this.organizations[0] : null;
                    this.organizations = this.organizations.filter(x => x.ID != id);
                }
            });
    }

    save(event) {
        this.organizationService.saveOrganization(event.item)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                if (result.type != 'error') {
                    this.showEditor = false;
                    this.getOrganizations();
                }
            });
    }

    closeEditor() {
        this.showEditor = false;
        if (this.organization == null) {
            this.organization = this.organizations.length > 0 ? this.organizations[0] : null;
        }
    }

    add() {
        this.showEditor = true;
        this.organization = null;
    }
    
    private openResource(event) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('resource', event.AcceptedBy));
    }
}