import { Component, Input, OnChanges, SimpleChange } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { Organization, OrganizationInvitation } from '../../../models/organization.model';
import { OrganizationsService } from '../../../services/organizations.service';
import { MessagesService } from '../../../services/messages.service';
import { BaseComponent } from '../../shared/base.component';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';

@Component({
    selector: 'd3s-admin-organization-invitations',
    providers: [OrganizationsService],
    template: `
               <header *ngIf="!showEditor && !showDelete">Invitations for this organization
                <d3s-tile-actions [hasAdd]="true" (addClick)="add()" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
               </header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <span *ngIf="!isLoading && !showDelete && !showEditor">
                    <input [hidden]="!showSimpleFilter" #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
                    <p-dataTable #dt [globalFilter]="gb" [value]="invitations" selectionMode="single" [rows]="defaultInitialItemsPerPage" paginator="true" pageLinks="3" [rowsPerPageOptions]="defaultPagingOptions" (onRowDblclick)="selected=$event.data;showEditor=true" [(selection)]="selected" >                                                                        
                        <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                        <p-column field="Email" header="Email" [sortable]="true" [filter]="!showSimpleFilter"></p-column>
                        <!--
                        <p-column field="Accepted" header="Accepted" [sortable]="true" [filter]="!showSimpleFilter" [style]="{width:'100px'}"></p-column>
                        <p-column field="DateAccepted" header="Accepted On" [sortable]="true" [filter]="!showSimpleFilter" [style]="{width:'120px'}"></p-column>
                        -->
                        <p-column [style]="{width:'40px'}">
                            <template let-item="rowData" pTemplate type="body">
                                <a (click)="openResource(item)">{{item.AcceptedByFirstName}} {{item.AcceptedByLastName}}</a>
                            </template>
                        </p-column>
                        <p-column [style]="{width:'40px'}">
                            <template let-item="rowData" pTemplate type="body">
                                <div class="RowTools">
                                    <a style="cursor:pointer;" (click)="selected=item;showEditor=true"><i class="fa fa-pencil"></i></a>                                        
                                </div>
                            </template>
                        </p-column>
                        <p-column  [style]="{width:'40px'}">
                            <template let-item="rowData" pTemplate type="body">
                                <div class="RowTools">                                
                                    <a style="cursor:pointer;" (click)="selected=item;showDelete=true"><i class="fa fa-trash-o"></i></a>                                    
                                </div>
                            </template>
                        </p-column>
                    </p-dataTable>  
                </span>
                <d3s-dynamic-editor *ngIf="showEditor" [objectID]="organization?.ID" [objectType]="'OrganizationInvitation'" [title]="'Organization Invitation'" [selection]="selected" (saveClick)="save($event)" (closeClick)="closeEditor()"></d3s-dynamic-editor> 
                <div style="padding:10px">                
                    <d3s-delete-form *ngIf="showDelete"
                        [callback]="theDeleteCallback"
                        [itemId]="selected?.ID"
                        [method]="'callback'"
                        [prompt]="'Are you sure you want to delete the invitation [' + selected?.Email + ']?'"                                         
                        (onCancel)="showDelete=false;"
                    ></d3s-delete-form> 
                </div> 

                `
})

export class AdminOrganizationInvitationsComponent extends BaseComponent implements OnChanges {
    @Input() organization: Organization = null;

    error: any;
    
    showEditor: boolean = false;
    showDelete: boolean = false;
    isLoading: boolean = false;

    invitations: OrganizationInvitation[] = [];
    selected: OrganizationInvitation;

    theDeleteCallback: Function;

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private organizationsService: OrganizationsService,
        private messagesService: MessagesService) {
        super();
        this.theDeleteCallback = this.delete.bind(this);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.organization != null) this.getInvitations();
    }

    getInvitations() {
        this.isLoading = true;
        this.organizationsService
            .getInvitationsByOrganization(this.organization.ID)
            .then(result => {
                this.invitations = result;
                this.selected = (this.invitations.length > 0 ? this.invitations[0] : null);                
                this.isLoading = false;
            })
            .catch(error => this.error = error);
    }

    delete(id: number) {
        this.organizationsService.deleteInvitation(id).then(result => {
            this.showMessageForResult(this.messagesService, result);
            if (result.type != 'error') this.invitations = this.invitations.filter(x => x.ID != id);
            this.showDelete = false;
        });
    }

    add() {
        this.showEditor = true;
        this.selected = null;
    }

    closeEditor() {
        this.showEditor = false;
        if (this.selected == null && this.invitations.length > 0)
            this.selected = this.invitations[0];
    }

    private openResource(event) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('resource', event.AcceptedBy));
    }
    
    save(event) {
        this.isLoading = true;
        this.organizationsService.saveInvitation(event.item)
            .then(result => {
                console.log(result);
                if (result.type != 'error') {
                    this.showEditor = false;
                    this.getInvitations();                
                }

                this.isLoading = false;
                this.showMessageForResult(this.messagesService, result);
            });        
    }    
}


