import { Component, Input, OnInit, SimpleChange } from '@angular/core';
import { Organization, ContractDetail, ContractType } from '../../../models/organization.model';
import { OrganizationsService } from '../../../services/organizations.service';
import { MessagesService } from '../../../services/messages.service';
import { BaseComponent } from '../../shared/base.component';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';

@Component({
    selector: 'd3s-admin-contracts',
    providers: [OrganizationsService],
    template: `
               <header *ngIf="!showEditor && !showDelete">Default Contracts
                <d3s-tile-actions [hasAdd]="true" (addClick)="add()" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
               </header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <span *ngIf="!isLoading && !showDelete && !showEditor">
                    <input [hidden]="!showSimpleFilter" #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
                    <p-dataTable #dt [globalFilter]="gb" [value]="contracts" selectionMode="single" [rows]="defaultInitialItemsPerPage" paginator="true" pageLinks="3" [rowsPerPageOptions]="defaultPagingOptions" (onRowDblclick)="selected=$event.data;showEditor=true" [(selection)]="selected" >                                                                        
                        <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>
                        <p-column field="Title" header="Title" [sortable]="true" [filter]="!showSimpleFilter"></p-column>
                        <p-column field="ContractTypeName" header="Type" [sortable]="true" [filter]="!showSimpleFilter"></p-column>
                        <p-column field="PublishedOn" header="Published On" [sortable]="true" [filter]="!showSimpleFilter">
                            <ng-template let-item="rowData" pTemplate type="body">
                                {{item.PublishedOn == null ? 'Never' : (item.PublishedOn | date : 'short')}}
                            </ng-template>
                        </p-column>
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
                <div *ngIf="showEditor">
                    <d3s-admin-organization-contract-editor 
                        [contractId]="selected?.ID" 
                        (onClose)="closeEditor()" 
                        (onSave)="closeEditor(); getContracts()">
                    </d3s-admin-organization-contract-editor>
                </div>
                <d3s-delete-form *ngIf="showDelete"
                    [callback]="theDeleteCallback"
                    [itemId]="selected?.ID"
                    [method]="'callback'"
                    [prompt]="'Are you sure you want to delete the contract [' + [selected?.Title] + ']?'"                                         
                    (onCancel)="showDelete=false;"
                ></d3s-delete-form>   

                `
})

export class AdminContractsComponent extends BaseComponent implements OnInit {
    error: any;
    
    showEditor: boolean = false;
    showDelete: boolean = false;
    isLoading: boolean = false;

    contracts: ContractDetail[] = [];
    selected: ContractDetail;

    theDeleteCallback: Function;

    constructor(
        private organizationsService: OrganizationsService,
        private messagesService: MessagesService) {
        super();
        this.theDeleteCallback = this.delete.bind(this);
    }

    ngOnInit() {
        this.getContracts();
    }

    getContracts() {
        this.isLoading = true;
        this.organizationsService
            .getDefaultContracts()
            .then(result => {
                this.contracts = result;
                this.selected = (this.contracts.length > 0 ? this.contracts[0] : null);                
                this.isLoading = false;
            })
            .catch(error => this.error = error);
    }

    delete(id: number) {
        this.organizationsService.deleteContract(id).then(result => {
            this.showMessageForResult(this.messagesService, result);
            if (result.type != 'error') this.contracts = this.contracts.filter(x => x.ID != id);
            this.showDelete = false;
        });
    }

    add() {
        this.showEditor = true;
        this.selected = null;
    }

    closeEditor() {
        this.showEditor = false;
        if (this.selected == null && this.contracts.length > 0)
            this.selected = this.contracts[0];
    }
}


