import { Component, Input, OnChanges, SimpleChange } from '@angular/core';
import { Organization, OrganizationDomain } from '../../../models/organization.model';
import { OrganizationsService } from '../../../services/organizations.service';
import { MessagesService } from '../../../services/messages.service';
import { BaseComponent } from '../../shared/base.component';

@Component({
    selector: 'd3s-admin-organization-domains',
    providers: [OrganizationsService],
    template: `
               <header *ngIf="!showEditor && !showDelete">Domains for this organization
                <d3s-tile-actions [hasAdd]="true" (addClick)="add()" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
               </header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <span *ngIf="!isLoading && !showDelete && !showEditor">
                    <input [hidden]="!showSimpleFilter" #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
                    <p-dataTable #dt [globalFilter]="gb" [value]="domains" selectionMode="single" [rows]="defaultInitialItemsPerPage" paginator="true" pageLinks="3" [rowsPerPageOptions]="defaultPagingOptions" (onRowDblclick)="selected=$event.data;showEditor=true" [(selection)]="selected" >                                                                        
                        <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                        <p-column field="Domain" header="Domain" [sortable]="true" [filter]="!showSimpleFilter"></p-column>
                        <p-column [style]="{width:'40px'}">
                            <ng-template let-item="rowData" pTemplate type="body">
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
                <d3s-dynamic-editor *ngIf="showEditor" [objectID]="organization?.ID" [objectType]="'OrganizationDomain'" [title]="'Organization Domain'" [rowID]="'ID'" [selection]="selected" (saveClick)="save($event)" (closeClick)="closeEditor()"></d3s-dynamic-editor> 
                <div style="padding: 10px">                
                    <d3s-delete-form *ngIf="showDelete"
                        [callback]="theDeleteCallback"
                        [itemId]="selected?.ID"
                        [method]="'callback'"
                        [prompt]="'Are you sure you want to delete the domain [' + selected?.Domain + ']?'"                                         
                        (onCancel)="showDelete=false;"
                    ></d3s-delete-form>  
                </div> 
                `
})

export class AdminOrganizationDomainsComponent extends BaseComponent implements OnChanges {
    @Input() organization: Organization = null;

    error: any;
    
    showEditor: boolean = false;
    showDelete: boolean = false;
    isLoading: boolean = false;

    domains: OrganizationDomain[] = [];
    selected: OrganizationDomain;

    theDeleteCallback: Function;

    constructor(
        private organizationsService: OrganizationsService,
        private messagesService: MessagesService) {
        super();
        this.theDeleteCallback = this.delete.bind(this);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.organization != null) this.getDomains();
    }

    getDomains() {
        this.isLoading = true;
        this.organizationsService
            .getDomainsByOrganization(this.organization.ID)
            .then(result => {
                this.domains = result;
                this.selected = (this.domains.length > 0 ? this.domains[0] : null);                
                this.isLoading = false;
            })
            .catch(error => this.error = error);
    }

    delete(id: number) {
        this.organizationsService.deleteDomain(id).then(result => {
            this.showMessageForResult(this.messagesService, result);
            if (result.type != 'error') this.domains = this.domains.filter(x => x.ID != id);
            this.showDelete = false;
        });
    }

    add() {
        this.showEditor = true;
        this.selected = null;
    }

    closeEditor() {
        this.showEditor = false;
        if (this.selected == null && this.domains.length > 0)
            this.selected = this.domains[0];
    }
    
    save(event) {
        this.showEditor = false;
        this.isLoading = true;
        this.organizationsService.saveDomain(event.item)
            .then(result => {
                this.isLoading = false;
                this.showMessageForResult(this.messagesService, result);
                this.getDomains();                
            });        
    }
    
}


