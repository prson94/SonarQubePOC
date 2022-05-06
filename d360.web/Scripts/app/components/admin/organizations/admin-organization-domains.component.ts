import { Component, Input, OnChanges, SimpleChange } from '@angular/core';

import { Organization, OrganizationDomain } from '../../../models/organization.model';

import { OrganizationsService } from '../../../services/organizations.service';

import { BaseComponent } from '../../shared/base.component';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../services/settings.service';
import '@angular/localize/init';

@Component({
    selector: 'd3s-admin-organization-domains',
    providers: [OrganizationsService],
    template: `
        <header *ngIf="!showEditor && !showDelete"><ng-container i18n>Domains for this organization</ng-container>
            <d3s-tile-actions [hasAdd]="true"
                              (addClick)="add()"
                              [hasFilterMode]="true"
                              [(filterMode)]="showSimpleFilter"></d3s-tile-actions>
        </header>
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <span *ngIf="!isLoading && !showDelete && !showEditor">
                    <input type="text"
                           [hidden]="!showSimpleFilter"
                           pInputText
                           size="100"
                           (input)="dt.filterGlobal($event.target.value, 'contains')"
                           placeholder="{{searchText}}"
                           class="grid-simple-filter">
                    <p-table #dt
                             [value]="domains"
                             selectionMode="single"
                             [metaKeySelection]="true"
                             [globalFilterFields]="['Domain']"
                             [pageLinks]="3"
                             [paginator]="true"
                             [rows]="defaultInitialItemsPerPage"
                             [rowsPerPageOptions]="defaultPagingOptions"
                             [(selection)]="selected">
                        <ng-template pTemplate="header">
                            <tr>
                                <th [pSortableColumn]="'Domain'">
                                    <ng-container i18n>Domain</ng-container>
                                    <d3s-sortIcon [field]="'Domain'"></d3s-sortIcon>
                                </th>
                                <th style="width: 40px"></th>
                                <th style="width: 40px"></th>
                            </tr>
                            <tr [hidden]="showSimpleFilter">
                                <th><d3s-column-filter [field]="'Domain'"
                                                       [datatype]="'text'"></d3s-column-filter></th>
                                <th></th>
                                <th></th>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="body"
                                     let-item>
                            <tr (dblclick)="selected=item;showEditor=true"
                                [pSelectableRow]="item">
                                <td>{{ item.Domain }}</td>
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
                            </tr>
                        </ng-template>
                        <ng-template *ngIf="dt.totalRecords"
                                     pTemplate="summary">
                            <d3s-grid-paging-info [first]="dt.first"
                                                  [rows]="dt.rows"
                                                  [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                        </ng-template>
                    </p-table>
                </span>
        <d3s-dynamic-editor *ngIf="showEditor"
                            [objectID]="organization?.ID"
                            [objectType]="'OrganizationDomain'"
                            [title]="editorTitle"
                            [rowID]="'ID'"
                            [selection]="selected"
                            (saveClick)="save($event)"
                            (closeClick)="closeEditor()"></d3s-dynamic-editor>
        <div style="padding: 10px">
            <d3s-delete-form *ngIf="showDelete"
                             [callback]="theDeleteCallback"
                             [itemId]="selected?.ID"
                             [method]="'callback'"
                             [prompt]="deleteText"
                             (onCancel)="showDelete=false;"
            ></d3s-delete-form>
        </div>
    `
})

export class AdminOrganizationDomainsComponent extends BaseComponent implements OnChanges {
    @Input() organization: Organization = null;

    error: any;

    showEditor = false;
    showDelete = false;
    isLoading = false;

    domains: OrganizationDomain[] = [];
    selected: OrganizationDomain;

    theDeleteCallback: Function;

    searchText = $localize`Search...`;
    editorTitle = $localize`Organization Domain`;

    constructor(
        private organizationsService: OrganizationsService,
        private messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService
    ) {
        super(settingsService);

        this.theDeleteCallback = this.delete.bind(this);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.organization != null) {
            this.getDomains();
        }
    }

    getDomains() {
        this.isLoading = true;
        this.organizationsService
            .getDomainsByOrganization(this.organization.ID)
            .subscribe(
                result => {
                    this.domains = result;

                    this.selected = (this.domains.length > 0 ? this.domains[0] : null);

                    this.isLoading = false;
                }
            );
    }

    delete(id: number) {
        this.organizationsService.deleteDomain(id).subscribe(
            result => {
                this.showMessageForResult(this.messagesService, result);

                if (result.type != 'error') {
                    this.domains = this.domains.filter(x => x.ID != id);
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
        if (this.selected == null && this.domains.length > 0) {
            this.selected = this.domains[0];
        }
    }

    save(event) {
        this.showEditor = false;
        this.isLoading = true;

        this.organizationsService
            .saveDomain(event.item)
            .subscribe(
                result => {
                    this.isLoading = false;
                    this.showMessageForResult(this.messagesService, result);
                    this.getDomains();
                }
            )
            ;
    }

    get deleteText(): string {
        return $localize`Are you sure you want to delete the domain [${this.selected?.Domain}]?`;
    }
}
