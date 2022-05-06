import { Component, Input, OnChanges, SimpleChange } from '@angular/core';
import { Router } from '@angular/router';

import { Organization, OrganizationInvitation } from '../../../models/organization.model';

import { OrganizationsService } from '../../../services/organizations.service';

import { BaseComponent } from '../../shared/base.component';

import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../services/settings.service';
import '@angular/localize/init';

@Component({
    selector: 'd3s-admin-organization-invitations',
    providers: [OrganizationsService],
    template: `
        <header *ngIf="!showEditor && !showDelete"><ng-container i18n>Invitations for this organization</ng-container>
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
                             [value]="invitations"
                             selectionMode="single"
                             [metaKeySelection]="true"
                             [globalFilterFields]="['Email']"
                             [pageLinks]="3"
                             [paginator]="true"
                             [rows]="defaultInitialItemsPerPage"
                             [rowsPerPageOptions]="defaultPagingOptions"
                             [(selection)]="selected">
                        <ng-template pTemplate="header">
                            <tr>
                                <th [pSortableColumn]="'Email'">
                                    <ng-container i18n>Email</ng-container>
                                    <d3s-sortIcon [field]="'Email'"></d3s-sortIcon>
                                </th>
                                <th style="width: 40px"></th>
                                <th style="width: 40px"></th>
                                <th style="width: 40px"></th>
                            </tr>
                            <tr [hidden]="showSimpleFilter">
                                <th><d3s-column-filter [field]="'Email'"
                                                       [datatype]="'text'"></d3s-column-filter></th>
                                <th></th>
                                <th></th>
                                <th></th>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="body"
                                     let-item>
                            <tr (dblclick)="selected=item;showEditor=true"
                                [pSelectableRow]="item">
                                <td>{{ item.Email }}</td>
                                <td>
                                    <a (click)="openResource(item)">{{ item.AcceptedByFirstName }} {{ item.AcceptedByLastName }}</a>
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
                            [objectType]="'OrganizationInvitation'"
                            [title]="editorTitle"
                            [selection]="selected"
                            (saveClick)="save($event)"
                            (closeClick)="closeEditor()"></d3s-dynamic-editor>
        <div style="padding:10px">
            <d3s-delete-form *ngIf="showDelete"
                             [callback]="theDeleteCallback"
                             [itemId]="selected?.ID"
                             [method]="'callback'"
                             [prompt]="deletePromptText"
                             (onCancel)="showDelete=false;"
            ></d3s-delete-form>
        </div>

    `
})

export class AdminOrganizationInvitationsComponent extends BaseComponent implements OnChanges {
    @Input() organization: Organization = null;

    error: any;

    showEditor = false;
    showDelete = false;
    isLoading = false;

    invitations: OrganizationInvitation[] = [];
    selected: OrganizationInvitation;

    theDeleteCallback: Function;

    searchText = $localize`Search...`;
    editorTitle = $localize`Organization Invitation`;

    constructor(
        private router: Router,
        private organizationsService: OrganizationsService,
        private messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService
    ) {
        super(settingsService);
        this.theDeleteCallback = this.delete.bind(this);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.organization != null) {
            this.getInvitations();
        }
    }

    getInvitations() {
        this.isLoading = true;
        this.organizationsService
            .getInvitationsByOrganization(this.organization.ID)
            .subscribe(
                (result) => {
                    this.invitations = result;
                    this.selected = (this.invitations.length > 0 ? this.invitations[0] : null);

                    this.isLoading = false;
                }
            )
            ;
    }

    delete(id: number) {
        this.organizationsService.deleteInvitation(id).subscribe(
            result => {
                this.showMessageForResult(this.messagesService, result);

                if (result.type != 'error') {
                    this.invitations = this.invitations.filter(x => x.ID != id);
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

        if (this.selected == null && this.invitations.length > 0) {
            this.selected = this.invitations[0];
        }
    }

    private openResource(event) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('resource', event.AcceptedBy));
    }

    save(event) {
        this.isLoading = true;
        this.organizationsService.saveInvitation(event.item).subscribe(
            result => {
                if (result.type != 'error') {
                    this.showEditor = false;
                    this.getInvitations();
                }

                this.showMessageForResult(this.messagesService, result);

                this.isLoading = false;
            }
        );
    }

    get deletePromptText(): string {
        return $localize`Are you sure you want to delete the invitation [${this.selected?.Email}]?`;
    }
}
