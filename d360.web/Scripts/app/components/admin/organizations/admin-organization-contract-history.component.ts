import {Component, Input, OnInit, Output, EventEmitter} from '@angular/core';
import {Router, ActivatedRoute} from '@angular/router';

import {ContractAcceptanceDetail} from '../../../models/organization.model';

import {OrganizationsService} from '../../../services/organizations.service';

import {BaseComponent} from '../../shared/base.component';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-admin-organization-contract-history',
    providers: [OrganizationsService],
    template: `
        <header><ng-container i18n>Contract History for {{ objectName }}</ng-container>
            <d3s-tile-actions [hasFilterMode]="true"
                              [(filterMode)]="showSimpleFilter"
                              [hasClose]="true"
                              (closeClick)="onClose.emit()"></d3s-tile-actions>
        </header>
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div *ngIf="!isLoading">
            <input type="text"
                   [hidden]="!showSimpleFilter"
                   pInputText
                   size="100"
                   (input)="dt.filterGlobal($event.target.value, 'contains')"
                   placeholder="searchText"
                   class="grid-simple-filter">
            <p-table #dt
                     [value]="contracts"
                     selectionMode="single"
                     [metaKeySelection]="true"
                     [globalFilterFields]="['ResourceName','ContractName','Accepted','AcceptedOn']"
                     [pageLinks]="3"
                     [paginator]="true"
                     [rows]="defaultInitialItemsPerPage"
                     [rowsPerPageOptions]="defaultPagingOptions"
                     [(selection)]="selected">
                <ng-template pTemplate="header">
                    <tr>
                        <th [pSortableColumn]="'ResourceName'">
                            <ng-container i18n>Resource Name</ng-container>
                            <d3s-sortIcon [field]="'ResourceName'"></d3s-sortIcon>
                        </th>
                        <th [pSortableColumn]="'ContractName'">
                            <ng-container i18n>Contract Name</ng-container>
                            <d3s-sortIcon [field]="'ContractName'"></d3s-sortIcon>
                        </th>
                        <th [pSortableColumn]="'Accepted'">
                            <ng-container i18n>Accepted</ng-container>
                            <d3s-sortIcon [field]="'Accepted'"></d3s-sortIcon>
                        </th>
                        <th [pSortableColumn]="'AcceptedOn'">
                            <ng-container i18n>Accepted On</ng-container>
                            <d3s-sortIcon [field]="'AcceptedOn'"></d3s-sortIcon>
                        </th>
                    </tr>
                    <tr [hidden]="showSimpleFilter">
                        <th>
                            <d3s-column-filter [field]="'ResourceName'"
                                               [datatype]="'text'"></d3s-column-filter>
                        </th>
                        <th>
                            <d3s-column-filter [field]="'ContractName'"
                                               [datatype]="'text'"></d3s-column-filter>
                        </th>
                        <th>
                            <d3s-column-filter [field]="'Accepted'"
                                               [datatype]="'text'"></d3s-column-filter>
                        </th>
                        <th></th>
                    </tr>
                </ng-template>
                <ng-template pTemplate="body"
                             let-item>
                    <tr (dblclick)="selected=item;showEditor=true"
                        [pSelectableRow]="item">
                        <td>{{ item.ResourceName }}</td>
                        <td>{{ item.ContractName }}</td>
                        <td>
                            <i *ngIf="item.Accepted == true"
                               class="fa fa-check enabled"
                               title="True"></i>
                            <i *ngIf="item.Accepted == false"
                               class="fa fa-times disabled"
                               title="False"></i>
                        </td>
                        <td>
                            <span>{{ item.AcceptedOn | date : 'short' }}</span>
                        </td>
                    </tr>
                </ng-template>
                <ng-template *ngIf="dt.totalRecords"
                             pTemplate="summary">
                    <d3s-grid-paging-info [first]="dt.first"
                                          [rows]="dt.rows"
                                          [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                </ng-template>
            </p-table>
        </div>

    `
})

export class AdminOrganizationContractHistoryComponent extends BaseComponent implements OnInit {
    @Input() id: number = null;
    @Input() type: string = null;
    @Input() objectName = '';
    @Output() onClose = new EventEmitter();

    error: any;
    isLoading = false;
    contracts: ContractAcceptanceDetail[] = [];

    searchText = $localize`Search...`;

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private organizationsService: OrganizationsService,
        private messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService
    ) {
        super(settingsService);
    }

    ngOnInit() {
        if (this.type != 'contract' && this.type != 'resource' && this.type != 'organization') {
            console.warn(`Invalid type ${this.type}`);
        }

        this.load();
    }

    load() {
        this.isLoading = true;

        switch (this.type.toLowerCase()) {
            case 'contract':
                this.organizationsService.getContractHistoryForContract(this.id)
                    .subscribe(
                        (r) => {
                            this.contracts = r;

                            this.isLoading = false;
                        }
                    );
                break;
            case 'resource':
                this.organizationsService.getContractHistoryForResource(this.id)
                    .subscribe(
                        r => {
                            this.contracts = r;

                            this.isLoading = false;
                        }
                    );
                break;
            case 'organization':
                this.organizationsService.getContractHistoryForOrganization(this.id)
                    .subscribe(
                        r => {
                            this.contracts = r;

                            this.isLoading = false;
                        }
                    );
                break;
        }
    }
}
