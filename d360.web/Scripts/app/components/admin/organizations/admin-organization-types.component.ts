import { Component, Input, OnInit, SimpleChange, Output, EventEmitter } from '@angular/core';
import { OrganizationType } from '../../../models/organization.model';
import { OrganizationsService } from '../../../services/organizations.service';
import { MessagesService } from '../../../services/messages.service';
import { BaseComponent } from '../../shared/base.component';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';

@Component({
    selector: 'd3s-admin-organization-types',
    providers: [OrganizationsService],
    template: `
               <header *ngIf="!showEditor && !showDelete">Organization Types
                <d3s-tile-actions [hasAdd]="true" (addClick)="add()" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
               </header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <span *ngIf="!isLoading && !showDelete && !showEditor">
                    <input [hidden]="!showSimpleFilter" #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
                    <p-dataTable #dt [globalFilter]="gb" [value]="organizationTypes" selectionMode="single" [rows]="defaultInitialItemsPerPage" paginator="true" pageLinks="3" [rowsPerPageOptions]="defaultPagingOptions" (onRowSelect)="type=$event.data;" (onRowDblclick)="type=$event.data;showEditor=true;typeChange.emit(type);" (selectionChange)="type=$event;typeChange.emit(type)" [selection]="type">
                        <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>
                        <p-column field="Name" header="Name" [sortable]="true" [filter]="!showSimpleFilter"></p-column>
                        <p-column field="OrganizationCount" header="Organization Count" [sortable]="true" [filter]="!showSimpleFilter" [style]="{width:'150px'}"></p-column>
                        <p-column [style]="{width:'40px'}">
                            <ng-template let-item="rowData" pTemplate type="body">
                                <div class="RowTools">
                                    <a style="cursor:pointer;" (click)="type=item;showEditor=true"><i class="fa fa-pencil"></i></a>                                        
                                </div>
                            </ng-template>
                        </p-column>
                        <p-column  [style]="{width:'40px'}">
                            <ng-template let-item="rowData" pTemplate type="body">
                                <div class="RowTools" *ngIf="item.OrganizationCount == 0">                                
                                    <a style="cursor:pointer;" (click)="type=item;showDelete=true"><i class="fa fa-trash-o"></i></a>                                    
                                </div>
                            </ng-template>
                        </p-column>
                    </p-dataTable>
                </span>                
                <d3s-asset-type-editor-form *ngIf="showEditor" [showParentPredicates]="false" [assetTypeClass]="'O'" [id]="type?.AssetTypeID" [title]="(type?'Edit':'Add') + ' Organization Type'" (onCancel)="cancel()" (onComplete)="actionComplete($event)"></d3s-asset-type-editor-form>                
                <d3s-delete-form *ngIf="showDelete"
                    [callback]="theDeleteCallback"
                    [itemId]="type?.AssetTypeID"
                    [method]="'callback'"
                    [prompt]="'Are you sure you want to delete the organization type [' + [type?.Name] + ']?'" 
                    (onCancel)="showDelete=false;"
                ></d3s-delete-form>
                `
})

export class AdminOrganizationTypesComponent extends BaseComponent implements OnInit {
    error: any;
    
    showEditor: boolean = false;
    showDelete: boolean = false;
    isLoading: boolean = false;

    @Input() type: OrganizationType = null;
    @Output() typeChange = new EventEmitter();

    organizationTypes: OrganizationType[] = [];

    theDeleteCallback: Function;

    constructor(
        private organizationsService: OrganizationsService,
        private messagesService: MessagesService) {
        super();
        this.theDeleteCallback = this.delete.bind(this);
    }

    ngOnInit() {
        this.getOrganizationTypes();
    }

    getOrganizationTypes() {
        this.isLoading = true;
        this.organizationsService
            .getOrganizationTypes()
            .then(result => {
                this.organizationTypes = result;
                this.type = (this.organizationTypes.length > 0 ? this.organizationTypes[0] : null);   
                this.typeChange.emit(this.type);
                this.isLoading = false;
            })
            .catch(error => this.error = error);
    }

    delete(id: number) {
        this.organizationsService.deleteOrganizationType(id).then(result => {
            this.showMessageForResult(this.messagesService, result);
            if (result.type != 'error') this.organizationTypes = this.organizationTypes.filter(x => x.AssetTypeID != id);
            this.showDelete = false;
        });
    }

    add() {
        this.showEditor = true;
        this.type = null;
    }

    cancel() {
        this.showEditor = false;
        if (this.type == null && this.organizationTypes.length > 0)
            this.type = this.organizationTypes[0];
    }
      
    actionComplete(event) {           
        this.showEditor = false;
        this.isLoading = false;
        this.getOrganizationTypes();
    }    
}