import { Component, Input, OnInit, Output, EventEmitter } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { Organization, OrganizationResource, ContractAcceptanceDetail } from '../../../models/organization.model';
import { OrganizationsService } from '../../../services/organizations.service';
import { MessagesService } from '../../../services/messages.service';
import { BaseComponent } from '../../shared/base.component';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';

@Component({
    selector: 'd3s-admin-organization-contract-history',
    providers: [OrganizationsService],
    template: `
               <header>Contract History for {{objectName}}
                <d3s-tile-actions [hasFilterMode]="true" [(filterMode)]="showSimpleFilter" [hasClose]="true" (closeClick)="onClose.emit()"></d3s-tile-actions>                            
               </header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div *ngIf="!isLoading">
                    <input [hidden]="!showSimpleFilter" #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
                    <p-dataTable #dt [globalFilter]="gb" [value]="contracts" selectionMode="single" [rows]="defaultInitialItemsPerPage" paginator="true" pageLinks="3" [rowsPerPageOptions]="defaultPagingOptions" (onRowDblclick)="selected=$event.data;showEditor=true" [(selection)]="selected" >                                                                        
                        <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>
                        <p-column field="ResourceName" header="Resource Name" [sortable]="true" [filter]="!showSimpleFilter"></p-column>
                        <p-column field="ContractName" header="Contract Name" [sortable]="true" [filter]="!showSimpleFilter" ></p-column>
                        <p-column field="Accepted" header="Accepted" [sortable]="true" [filter]="!showSimpleFilter">
                            <ng-template let-col let-item="rowData" pTemplate type="body">
                                <i *ngIf="item.Accepted == true" class="fa fa-check enabled" title="True"></i>
                                <i *ngIf="item.Accepted == false" class="fa fa-times disabled" title="False"></i>
                            </ng-template>
                        </p-column>
                        <p-column field="AcceptedOn" header="Accepted On" [sortable]="true">
                            <ng-template let-col let-item="rowData" pTemplate type="body">
                                <span>{{item.AcceptedOn | date : 'short'}}</span>
                            </ng-template>
                        </p-column>
                    </p-dataTable>  
                </div>

                `
})

export class AdminOrganizationContractHistoryComponent extends BaseComponent implements OnInit {
    @Input() id: number = null;
    @Input() type: string = null;
    @Input() objectName: string = '';
    @Output() onClose = new EventEmitter();

    error: any;
    isLoading: boolean = false;
    contracts: ContractAcceptanceDetail[] = [];

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private organizationsService: OrganizationsService,
        private messagesService: MessagesService) {
        super();
    }

    ngOnInit() {
        if (this.type != 'contract' && this.type != 'resource' && this.type != 'organization')
            console.warn(`Invalid type ${this.type}`);
        this.load();
    }


    load() {
        this.isLoading = true;

        switch (this.type.toLowerCase()) {
            case 'contract':
                this.organizationsService.getContractHistoryForContract(this.id)
                    .then(r => {
                        this.contracts = r;
                        this.isLoading = false;
                    })
                break;
            case 'resource':
                this.organizationsService.getContractHistoryForResource(this.id)
                    .then(r => {
                        this.contracts = r;
                        this.isLoading = false;
                    })
                break;
            case 'organization':
                this.organizationsService.getContractHistoryForOrganization(this.id)
                    .then(r => {
                        this.contracts = r;
                        this.isLoading = false;
                    })
                break;
        }

    }
}


