import { Component, OnInit, OnDestroy} from '@angular/core';
import { Router } from '@angular/router';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../../services/right-sidebar.service';
import { OrganizationsService } from '../../../services/organizations.service';
import { MessagesService } from '../../../services/messages.service';
import { StateService } from '../../../services/state.service';
import { AdminBaseComponent } from '../admin-base.component';
import { Organization } from '../../../models/organization.model';
import { Title } from '@angular/platform-browser';
import { RightSidebarItem } from '../../../models/rightsidebar.model';

@Component({
    selector: 'd3s-admin-organizations-component',
    providers: [OrganizationsService],
    template: `
<div class="col l6 s12">          
    <div class="tile tile-detail">
        <header *ngIf="!showEditor && !showDelete">
            Organizations
            <d3s-tile-actions [hasAdd]="true" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter" (addClick)="add()"></d3s-tile-actions>
        </header>
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <span *ngIf="!isLoading && !showEditor && !showDelete">
            <input #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
            <p-dataTable #dt sortField="Name" [sortOrder]="1" [globalFilter]="gb" [value]="organizations" selectionMode="single" [rows]="20" [paginator]="true" [pageLinks]="3" expandableRows="true" [(selection)]="selected"  (onRowDblclick)="selected=$event.data;showEditor=true;">
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
                            <a style="cursor:pointer;" (click)="selected=item;showEditor=true"><i class="fa fa-pencil"></i></a>
                        </div>
                    </ng-template>
                </p-column>
                <p-column  [style]="{width:'40px'}">
                    <ng-template let-item="rowData" pTemplate type="body">
                        <div class="RowTools">
                            <a style="cursor:pointer;" (click)="selected=item;showDelete=true"><i class="fa fa-trash-o"></i></a>
                        </div>
                    </ng-template>
                </p-column>
            </p-dataTable>
        </span>
        <d3s-dynamic-editor *ngIf="showEditor" [objectID]="selected?.ID" [objectType]="'Organization'" [title]="'Organization'" [selection]="selected" (saveClick)="save($event)" (closeClick)="closeEditor()"></d3s-dynamic-editor>     
        <d3s-delete-form *ngIf="showDelete"
            [callback]="theDeleteCallback"
            [itemId]="selected?.ID"
            [method]="'callback'"
            [prompt]="'Are you sure you want to delete the organization [' + [selected?.Name] + ']?'"                                         
            (onCancel)="showDelete=false;"
        ></d3s-delete-form>
    </div>
    <div class="tile tile-detail"> 
        <d3s-admin-contracts></d3s-admin-contracts>
    </div>
</div>
<div class="col l6 s12" *ngIf="!showEditor && !showDelete && selected">
    <div class="row">
        <div class="col s12">
            <div class="tile tile-detail">  
                <d3s-admin-organization-contracts [organization]="selected"></d3s-admin-organization-contracts>
            </div>
        </div>
        <div class="col s12">
            <div class="tile tile-detail">  
                <d3s-admin-organization-domains [organization]="selected"></d3s-admin-organization-domains>
            </div>
        </div>
        <div class="col s12">
            <div class="tile tile-detail">  
                <d3s-admin-organization-invitations [organization]="selected"></d3s-admin-organization-invitations>
            </div>
        </div>
        <div class="col s12">
            <div class="tile tile-detail">  
                <d3s-admin-organization-resources [organization]="selected"></d3s-admin-organization-resources>
            </div>
        </div>
    </div>
<div>
`
})

export class AdminOrganizationsComponent extends AdminBaseComponent implements OnInit, OnDestroy {
    organizations: Organization[] = [];
    selected: Organization;
    showEditor: boolean = false;
    showDelete: boolean = false;
    theDeleteCallback: Function;
    isClassificationsVisible: boolean = false;

    constructor(private router: Router, private stateService: StateService, rightSidebarService: RightSidebarService, private organizationService: OrganizationsService, protected messagesService: MessagesService, headerBreadcrumbService: HeaderBreadcrumbService, titleService: Title) {
        super(headerBreadcrumbService, titleService, rightSidebarService);        
        this.areaName = "Organizations";
        this.setCommonItems();
        this.theDeleteCallback = this.deleteOrganization.bind(this);
        this.setCommonRightSideBar(true);

    }

    ngOnInit() {
        this.getOrganizations();
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    getOrganizations() {
        this.isLoading = true;
        this.organizationService.getOrganizations()
            .then(result => {
                this.organizations = result;
                this.isLoading = false;
                if (this.organizations.length > 0) this.selected = this.organizations[0];
            });
    }
        
    deleteOrganization(id: number) {
        this.organizationService.deleteOrganization(id)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                this.showDelete = false;
                if (result.type != 'error') {
                    this.selected = this.organizations.length > 0 ? this.organizations[0] : null;
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

                this.stateService.reloadLeftNavMenu();
            });
    }

    closeEditor() {
        this.showEditor = false;
        if (this.selected == null) {
            this.selected = this.organizations.length > 0 ? this.organizations[0] : null;
        }
    }

    add() {
        this.showEditor = true;
        this.selected = null;
    }
    
    private openResource(event) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('resource', event.AcceptedBy));
    }
}