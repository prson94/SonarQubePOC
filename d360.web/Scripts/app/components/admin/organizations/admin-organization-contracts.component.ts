import {
    Component,
    Input,
    OnChanges,
    SimpleChange
} from '@angular/core';

import { Organization, ContractDetail } from '../../../models/organization.model';

import { OrganizationsService } from '../../../services/organizations.service';

import { BaseComponent } from '../../shared/base.component';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-admin-organization-contracts',
    providers: [OrganizationsService],
    template: `
        <header *ngIf="!showEditor && !showDelete && !showHistory"><ng-container i18n>Contracts for this organization</ng-container>
            <d3s-tile-actions [hasAdd]="true"
                              (addClick)="add()"
                              [hasFilterMode]="true"
                              [(filterMode)]="showSimpleFilter"></d3s-tile-actions>
        </header>
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div *ngIf="!isLoading && !showDelete && !showEditor && !showHistory">
            <input type="text"
                   [hidden]="!showSimpleFilter"
                   pInputText
                   size="100"
                   (input)="dt.filterGlobal($event.target.value, 'contains')"
                   placeholder="{{searchText}}"
                   class="grid-simple-filter">
            <p-table #dt
                     [value]="contracts"
                     selectionMode="single"
                     [metaKeySelection]="true"
                     [globalFilterFields]="['Title','ContractTypeName','PublishedOn']"
                     [pageLinks]="3"
                     [paginator]="true"
                     [rows]="defaultInitialItemsPerPage"
                     [rowsPerPageOptions]="defaultPagingOptions"
                     [(selection)]="selected">
                <ng-template pTemplate="header">
                    <tr>
                        <th [pSortableColumn]="'Title'">
                            <ng-container i18n>Title</ng-container>
                            <d3s-sortIcon [field]="'Title'"></d3s-sortIcon>
                        </th>
                        <th [pSortableColumn]="'ContractTypeName'"
                            style="width: 220px">
                            <ng-container i18n>Type</ng-container>
                            <d3s-sortIcon [field]="'ContractTypeName'"></d3s-sortIcon>
                        </th>
                        <th [pSortableColumn]="'PublishedOn'">
                            <ng-container i18n>Published On</ng-container>
                            <d3s-sortIcon [field]="'PublishedOn'"></d3s-sortIcon>
                        </th>
                        <th style="width: 40px"></th>
                        <th style="width: 40px"></th>
                        <th style="width: 40px"></th>
                    </tr>
                    <tr [hidden]="showSimpleFilter">
                        <th>
                            <d3s-column-filter [field]="'Title'"
                                               [datatype]="'text'"></d3s-column-filter>
                        </th>
                        <th>
                            <d3s-column-filter [field]="'ContractTypeName'"
                                               [datatype]="'text'"></d3s-column-filter>
                        </th>
                        <th>
                            <d3s-column-filter [field]="'PublishedOn'"
                                               [datatype]="'text'"></d3s-column-filter>
                        </th>
                        <th></th>
                        <th></th>
                        <th></th>
                    </tr>
                </ng-template>
                <ng-template pTemplate="body"
                             let-item>
                    <tr (dblclick)="selected=item;showEditor=true"
                        [pSelectableRow]="item">
                        <td>{{ item.Title }}</td>
                        <td>{{ item.ContractTypeName }}</td>
                        <td>
                            {{ item.PublishedOn == null ? labelNever : (item.PublishedOn | date : 'short') }}
                        </td>
                        <td>
                            <div class="RowTools">
                                <a style="cursor:pointer;"
                                   (click)="selected=item;showEditor=true"><i class="fa fa-pencil"></i></a>
                            </div>
                        </td>
                        <td>
                            <div class="RowTools">
                                <a style="cursor:pointer;"
                                   (click)="selected=item;showDelete=true"><i class="fa fa-trash-o"></i></a>
                            </div>
                        </td>
                        <td>
                            <div class="RowTools">
                                <a style="cursor:pointer;"
                                   (click)="selected=item;showHistory=true"><i class="fa fa-history"></i></a>
                            </div>
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
        <div *ngIf="showEditor">
            <d3s-admin-organization-contract-editor
                    [contractId]="selected?.ID"
                    [organizationId]="organization?.ID"
                    (onClose)="showEditor = false"
                    (onSave)="showEditor = false; getContracts()">
            </d3s-admin-organization-contract-editor>
        </div>
        <d3s-delete-form *ngIf="showDelete"
                         [callback]="theDeleteCallback"
                         [itemId]="selected?.ID"
                         [method]="'callback'"
                         [prompt]="deletePrompt"
                         (onCancel)="showDelete=false;"
        ></d3s-delete-form>
        <div *ngIf="showHistory">
            <d3s-admin-organization-contract-history type="contract"
                                                     [id]="selected?.ID"
                                                     [objectName]="(selected?.Title || 'Contract')"
                                                     (onClose)="showHistory = false"></d3s-admin-organization-contract-history>
        </div>
    `
})

export class AdminOrganizationContractsComponent extends BaseComponent implements OnChanges {
    @Input() organization: Organization = null;

    error: any;

    showEditor = false;
    showDelete = false;
    showHistory = false;
    isLoading = false;

    contracts: ContractDetail[] = [];
    selected: ContractDetail;

    theDeleteCallback: Function;

    searchText = $localize`Search...`;
    labelNever = $localize`Never`;


    constructor(
        private organizationsService: OrganizationsService,
        private messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService
    ) {
        super(settingsService);

        this.theDeleteCallback = this.delete.bind(this);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes['organization']) {
            if (this.organization != null) {
                this.getContracts();
            } else {
                this.contracts = [];
            }
        }

    }

    getContracts() {
        this.isLoading = true;

        this.organizationsService
            .getContractsByOrganization(this.organization.ID)
            .subscribe(
                result => {
                    this.contracts = result;

                    this.selected = (this.contracts.length > 0 ? this.contracts[0] : null);

                    this.isLoading = false;
                }
            )
            ;
    }

    delete(id: number) {
        this.organizationsService.deleteContract(id).subscribe(
            result => {
                this.showMessageForResult(this.messagesService, result);

                if (result.type != 'error') {
                    this.contracts = this.contracts.filter(x => x.ID != id);
                }

                this.showDelete = false;
            }
        );
    }

    add() {
        this.showEditor = true;
        this.selected = null;
    }

    closeEditor() {
        this.showEditor = false;

        if (this.selected == null && this.contracts.length > 0) {
            this.selected = this.contracts[0];
        }
    }

    get deletePrompt(): string {
        return $localize`Are you sure you want to delete the contract [${this.selected?.Title}]?`;
    }
}
