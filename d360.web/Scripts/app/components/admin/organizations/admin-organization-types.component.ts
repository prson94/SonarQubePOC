import { Component, Input, OnInit, SimpleChange, Output, EventEmitter } from '@angular/core';
import { OrganizationType } from '../../../models/organization.model';
import { OrganizationsService } from '../../../services/organizations.service';
import { BaseComponent } from '../../shared/base.component';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { AssetTypeClass } from '../../../models/asset.model';
import { CompanySettingsService } from '../../../services/settings.service';
import '@angular/localize/init';

@Component({
    selector: 'd3s-admin-organization-types',
    providers: [OrganizationsService],
    template: `
        <header *ngIf="!showEditor && !showDelete"><ng-container i18n>Organization Types</ng-container>
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
                             [value]="organizationTypes"
                             selectionMode="single"
                             [metaKeySelection]="true"
                             [globalFilterFields]="['Name','OrganizationCount']"
                             [pageLinks]="3"
                             [paginator]="true"
                             [rows]="defaultInitialItemsPerPage"
                             [rowsPerPageOptions]="defaultPagingOptions"
                             (selectionChange)="type=$event;typeChange.emit(type)"
                             [selection]="type">
                        <ng-template pTemplate="header">
                            <tr>
                                <th [pSortableColumn]="'Name'">
                                    <ng-container i18n>Name</ng-container>
                                    <d3s-sortIcon [field]="'Name'"></d3s-sortIcon>
                                </th>
                                <th [pSortableColumn]="'OrganizationCount'"
                                    style="width: max-150px">
                                    <ng-container i18n>Organization Count</ng-container>
                                    <d3s-sortIcon [field]="'OrganizationCount'"></d3s-sortIcon>
                                </th>
                                <th style="width: 40px"></th>
                                <th style="width: 40px"></th>
                            </tr>
                            <tr [hidden]="showSimpleFilter">
                                <th><d3s-column-filter [field]="'Name'"
                                                       [datatype]="'text'"></d3s-column-filter></th>
                                <th><d3s-column-filter [field]="'OrganizationCount'"
                                                       [datatype]="'text'"></d3s-column-filter></th>
                                <th></th>
                                <th></th>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="body"
                                     let-item>
                            <tr (dblclick)="type=item;showEditor=true;typeChange.emit(type);"
                                [pSelectableRow]="item">
                                <td>{{ item.Name }}</td>
                                <td>{{ item.OrganizationCount }}</td>
                                <td>
                                    <div class="RowTools">
                                        <a style="cursor:pointer;"
                                           (click)="type=item;showEditor=true"><i class="fa fa-pencil"></i></a>
                                    </div>
                                </td>
                                <td>
                                    <div class="RowTools"
                                         *ngIf="item.OrganizationCount == 0">
                                        <a style="cursor:pointer;"
                                           (click)="type=item;showDelete=true"><i class="fa fa-trash-o"></i></a>
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
        <d3s-asset-type-editor *ngIf="showEditor"
                               [assetTypeClass]="assetTypeClass"
                               [id]="type?.AssetTypeID"
                               [title]="editorTitle"
                               (onCancel)="cancel()"
                               (onComplete)="actionComplete($event)"></d3s-asset-type-editor>
        <d3s-delete-form *ngIf="showDelete"
                         [callback]="theDeleteCallback"
                         [itemId]="type?.ID"
                         [method]="'callback'"
                         [prompt]="deletePromptText"
                         (onCancel)="showDelete=false;"
        ></d3s-delete-form>
    `
})

export class AdminOrganizationTypesComponent extends BaseComponent implements OnInit {
    error: any;

    showEditor = false;
    showDelete = false;
    isLoading = false;
    assetTypeClass: AssetTypeClass = AssetTypeClass.Organization;

    @Input() type: OrganizationType = null;
    @Output() typeChange = new EventEmitter();

    organizationTypes: OrganizationType[] = [];

    theDeleteCallback: Function;

    searchText = $localize`Search...`;

    get deletePromptText(): string {
        return $localize`Are you sure you want to delete the organization type [${this.type?.Name}]?`;
    }

    get editorTitle(): string {
        if (this.type) {
            return $localize`Edit Organization Type`;
        }
        return $localize`Add Organization Type`;
    }

    constructor(
        private organizationsService: OrganizationsService,
        private messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService
    ) {
        super(settingsService);

        this.theDeleteCallback = this.delete.bind(this);
    }

    ngOnInit() {
        this.getOrganizationTypes();
    }

    getOrganizationTypes() {
        this.isLoading = true;

        this.organizationsService
            .getOrganizationTypes()
            .subscribe(
                result => {
                    this.organizationTypes = result;
                    this.type = (this.organizationTypes.length > 0 ? this.organizationTypes[0] : null);
                    this.typeChange.emit(this.type);

                    this.isLoading = false;
                }
            );
    }

    delete(id: number) {
        this.organizationsService.deleteOrganizationType(id).subscribe(
            result => {
                this.showMessageForResult(this.messagesService, result);

                if (result.type != 'error') {
                    this.organizationTypes = this.organizationTypes.filter((x) => x.ID != id);
                }

                this.showDelete = false;
            }
        );
    }

    add() {
        this.showEditor = true;
        this.type = null;
    }

    cancel() {
        this.showEditor = false;

        if (this.type == null && this.organizationTypes.length > 0) {
            this.type = this.organizationTypes[0];
        }
    }

    actionComplete(event) {
        this.showEditor = false;
        this.getOrganizationTypes();

        this.isLoading = false;
    }
}
