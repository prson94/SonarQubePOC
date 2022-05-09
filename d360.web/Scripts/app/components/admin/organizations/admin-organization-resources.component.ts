import { Component, Input, OnChanges, SimpleChange } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';

import { Organization, OrganizationResource } from '../../../models/organization.model';

import { OrganizationsService } from '../../../services/organizations.service';

import { BaseComponent } from '../../shared/base.component';

import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-admin-organization-resources',
    providers: [OrganizationsService],
    template: `
        <header *ngIf="!showHistory"><ng-container i18n>Users for this organization</ng-container>
            <d3s-tile-actions [hasFilterMode]="true"
                              [(filterMode)]="showSimpleFilter"></d3s-tile-actions>
        </header>
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div *ngIf="!isLoading && !showHistory">
            <input type="text"
                   [hidden]="!showSimpleFilter"
                   pInputText
                   size="100"
                   (input)="dt.filterGlobal($event.target.value, 'contains')"
                   placeholder="{{searchText}}"
                   class="grid-simple-filter">
            <p-table #dt
                     [value]="resources"
                     selectionMode="single"
                     [metaKeySelection]="true"
                     [globalFilterFields]="['Email','Status','Accepted','DateAccepted','DateLastLoggedIn']"
                     [pageLinks]="3"
                     [paginator]="true"
                     [rows]="defaultInitialItemsPerPage"
                     [rowsPerPageOptions]="defaultPagingOptions"
                     [(selection)]="selected">
                <ng-template pTemplate="header">
                    <tr>
                        <th style="max-width: 140px">Name</th>
                        <th [pSortableColumn]="'Email'">
                            <ng-container i18n>Email</ng-container>
                            <d3s-sortIcon [field]="'Email'"></d3s-sortIcon>
                        </th>
                        <th [pSortableColumn]="'Status'"
                            style="max-width: 100px">
                            <ng-container i18n>Status</ng-container>
                            <d3s-sortIcon [field]="'Status'"></d3s-sortIcon>
                        </th>
                        <th [pSortableColumn]="'Accepted'"
                            style="max-width: 100px">
                            <ng-container i18n>Accepted</ng-container>
                            <d3s-sortIcon [field]="'Accepted'"></d3s-sortIcon>
                        </th>
                        <th [pSortableColumn]="'DateAccepted'"
                            style="max-width: 150px">
                            <ng-container i18n>Accepted On</ng-container>
                            <d3s-sortIcon [field]="'DateAccepted'"></d3s-sortIcon>
                        </th>
                        <th [pSortableColumn]="'DateLastLoggedIn'"
                            style="max-width: 120px">
                            <ng-container i18n>Last Logon</ng-container>
                            <d3s-sortIcon [field]="'DateLastLoggedIn'"></d3s-sortIcon>
                        </th>
                        <th style="width: 40px"></th>
                    </tr>
                    <tr [hidden]="showSimpleFilter">
                        <th></th>
                        <th>
                            <d3s-column-filter [field]="'Email'"
                                               [datatype]="'text'"></d3s-column-filter>
                        </th>
                        <th>
                            <d3s-column-filter [field]="'Status'"
                                               [datatype]="'text'"></d3s-column-filter>
                        </th>
                        <th>
                            <d3s-column-filter [field]="'Accepted'"
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
                        <td>
                            <a (click)="openResource(item)">{{ item.FirstName }} {{ item.LastName }}</a>
                        </td>
                        <td>{{ item.Email }}</td>
                        <td>{{ item.Status }}</td>
                        <td>
                            <i *ngIf="item.Accepted == true"
                               class="fa fa-check enabled"
                               title="True"></i>
                            <i *ngIf="item.Accepted == false"
                               class="fa fa-times disabled"
                               title="False"></i>
                        </td>
                        <td>
                            <span>{{ item.DateAccepted | date : 'short' }}</span>
                        </td>
                        <td>
                            <span>{{ item.DateLastLoggedIn | date : 'short' }}</span>
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
        <div *ngIf="!isLoading && showHistory">
            <d3s-admin-organization-contract-history type="resource"
                                                     [id]="selected?.ResourceID"
                                                     [objectName]="(selected?.FirstName || '') + ' ' + (selected?.LastName || '')"
                                                     (onClose)="showHistory = false"></d3s-admin-organization-contract-history>
        </div>
    `
})

export class AdminOrganizationResourcesComponent extends BaseComponent implements OnChanges {
    @Input() organization: Organization = null;

    error: any;
    isLoading = false;
    showHistory = false;

    resources: OrganizationResource[] = [];
    selected: OrganizationResource;

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

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.organization != null) {
            this.getResources();
        }
    }

    getResources() {
        this.isLoading = true;
        this.organizationsService
            .getUsersByOrganization(this.organization.ID)
            .subscribe(
                result => {
                    this.resources = result;

                    this.selected = (this.resources.length > 0 ? this.resources[0] : null);

                    this.isLoading = false;
                }
            )
            ;
    }

    private openResource(event) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('resource', event.ResourceID));
    }
}
