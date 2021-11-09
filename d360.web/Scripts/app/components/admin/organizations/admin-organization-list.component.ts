import {Component, Input, OnChanges, SimpleChange, Output, EventEmitter} from '@angular/core';
import {Router, ActivatedRoute} from '@angular/router';

import {Organization, OrganizationType} from '../../../models/organization.model';

import {OrganizationsService} from '../../../services/organizations.service';

import {BaseComponent} from '../../shared/base.component';

import {SiteUrlHelpers} from '../../../static/site-url-helpers';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-admin-organization-list-component',
    providers: [OrganizationsService],
    template: `
        <div class="tile tile-detail">
            <header *ngIf="!showEditor && !showDelete && !showHistory">
                Organizations
                <d3s-tile-actions [hasAdd]="true"
                                  [hasFilterMode]="true"
                                  [(filterMode)]="showSimpleFilter"
                                  (addClick)="add()"></d3s-tile-actions>
            </header>
            <d3s-loading [isLoading]="isLoading"></d3s-loading>
            <div *ngIf="!isLoading && !showEditor && !showDelete && !showHistory">
                <input type="text"
                       [hidden]="!showSimpleFilter"
                       pInputText
                       size="100"
                       (input)="dt.filterGlobal($event.target.value, 'contains')"
                       placeholder="Search..."
                       class="grid-simple-filter">
                <p-table #dt
                         [value]="organizations"
                         selectionMode="single"
                         [metaKeySelection]="true"
                         [selection]="organization"
                         (selectionChange)="organization=$event;organizationChange.emit($event);"
                         [globalFilterFields]="['Name','AdministratorEmail','DateAccepted','AcceptedBy']"
                         sortField="Name"
                         [sortOrder]="1"
                         [pageLinks]="3"
                         [paginator]="true"
                         [rows]="20"
                         (onRowSelect)="organization=$event.data;organizationChange.emit(organization);">
                    <ng-template pTemplate="header">
                        <tr>
                            <th [pSortableColumn]="'Name'">
                                Name
                                <d3s-sortIcon [field]="'Name'"></d3s-sortIcon>
                            </th>
                            <th>Administrator Email</th>
                            <th>Accepted On</th>
                            <th>Accepted By</th>
                            <th style="width: 40px"></th>
                            <th style="width: 40px"></th>
                            <th style="width: 40px"></th>
                        </tr>
                        <tr [hidden]="showSimpleFilter">
                            <th>
                                <d3s-column-filter [field]="'Name'"
                                                   [datatype]="'text'"></d3s-column-filter>
                            </th>
                            <th></th>
                            <th></th>
                            <th></th>
                            <th></th>
                            <th></th>
                            <th></th>
                        </tr>
                    </ng-template>
                    <ng-template pTemplate="body"
                                 let-item>
                        <tr (dblclick)="organization=item;showEditor=true;organizationChange.emit(organization);"
                            [pSelectableRow]="item">
                            <td>{{ item.Name }}</td>
                            <td>{{ item.AdministratorEmail }}</td>
                            <td>
                                {{ item.DateAccepted | date: 'short' }}
                            </td>
                            <td>
                                <a (click)="openResource(item)">{{ item.AcceptedByName }}</a>
                            </td>
                            <td>
                                <div class="RowTools">
                                    <a style="cursor:pointer;"
                                       (click)="organization=item;showEditor=true"><i class="fa fa-pencil"></i></a>
                                </div>
                            </td>
                            <td>
                                <div class="RowTools">
                                    <a style="cursor:pointer;"
                                       (click)="organization=item;showDelete=true"><i class="fa fa-trash-o"></i></a>
                                </div>
                            </td>
                            <td>
                                <div class="RowTools">
                                    <a style="cursor:pointer;"
                                       (click)="organization=item;showHistory=true"><i class="fa fa-history"></i></a>
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
            <d3s-dynamic-editor *ngIf="showEditor"
                                [objectID]="organizationType?.ID"
                                [objectType]="'Organization'"
                                [title]="'Organization'"
                                [selection]="organization"
                                (saveClick)="save($event)"
                                (closeClick)="closeEditor()"></d3s-dynamic-editor>
            <d3s-delete-form *ngIf="showDelete"
                             [callback]="theDeleteCallback"
                             [itemId]="organization?.ID"
                             [method]="'callback'"
                             [prompt]="'Are you sure you want to delete the organization [' + [organization?.Name] + ']?'"
                             (onCancel)="showDelete=false;"
            ></d3s-delete-form>
            <div *ngIf="showHistory">
                <d3s-admin-organization-contract-history type="organization"
                                                         [id]="organization?.ID"
                                                         [objectName]="(organization?.Name || 'Organization')"
                                                         (onClose)="showHistory = false"></d3s-admin-organization-contract-history>
            </div>
        </div>
    `
})

export class AdminOrganizationListComponent extends BaseComponent implements OnChanges {
    @Input() organizationType: OrganizationType = null;
    @Input() organization: Organization;
    @Output() organizationChange = new EventEmitter();

    organizations: Organization[] = [];
    showEditor = false;
    showDelete = false;
    showHistory = false;
    theDeleteCallback: Function;

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private organizationService: OrganizationsService,
        private messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService
    ) {
        super(settingsService);

        this.theDeleteCallback = this.deleteOrganization.bind(this);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes['organizationType']) {
            if (this.organizationType != null) {
                this.getOrganizations();
            } else {
                this.organizations = [];

                if (this.organization != null) {
                    this.organization = null;
                    this.organizationChange.emit(null);
                }
            }
        }
    }

    getOrganizations() {
        this.isLoading = true;

        this.organizations = [];

        this.organizationService.getOrganizationsByType(this.organizationType.ID).subscribe(
            result => {
                this.organizations = result;
                this.isLoading = false;

                if (this.organizations.length > 0) {
                    this.organization = this.organizations[0];
                } else {
                    this.organization = null;
                }

                this.organizationChange.emit(this.organization);
            }
        );
    }

    deleteOrganization(id: number) {
        this.organizationService.deleteOrganization(id).subscribe(
            result => {
                this.showMessageForResult(this.messagesService, result);
                this.showDelete = false;

                if (result.type != 'error') {
                    this.organization = this.organizations.length > 0 ? this.organizations[0] : null;
                    this.organizations = this.organizations.filter(x => x.ID != id);
                }
            }
        );
    }

    save(event) {
        this.organizationService.saveOrganization(event.item).subscribe(
            result => {
                this.showMessageForResult(this.messagesService, result);

                if (result.type != 'error') {
                    this.showEditor = false;
                    this.getOrganizations();
                }
            }
        );
    }

    closeEditor() {
        this.showEditor = false;

        if (this.organization == null) {
            this.organization = this.organizations.length > 0 ? this.organizations[0] : null;
        }
    }

    add() {
        this.showEditor = true;
        this.organization = null;
    }

    private openResource(event) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('resource', event.AcceptedBy));
    }
}
