import {debounceTime} from 'rxjs/operators';

import {Title} from '@angular/platform-browser';
import {ActivatedRoute, Router} from '@angular/router';
import {Breadcrumb} from '../../models/breadcrumb.model';
import {GridColumn, GridField, GridFilterExpression} from '../../models/grid-definition.model';
import {GridDefinitionService} from '../../services/grid-definition.service';
import {HeaderBreadcrumbService} from '../../services/header-breadcrumb.service';
import {MessagesService} from '../../services/messages.service';
import {PermissionsService} from '../../services/permissions.service';
import {ResourcesService} from '../../services/resources.service';
import {CompanySettingsService} from '../../services/settings.service';
import {UriBasedService} from '../../services/uri-based.service';
import {SiteUrlHelpers} from '../../static/site-url-helpers';
import {BaseComponent} from '../shared/base.component';
import {LazyLoadEvent} from 'primeng/primeng';
import {SubscriptionLike as ISubscription} from 'rxjs';
import {SortOrder} from '../../models/enums.model';
import {ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, ViewChild, OnInit} from '@angular/core';
/* FIXME: Extract templates and styles to their own files
*  https://angular.io/guide/styleguide#style-05-04 */
@Component({
    selector: 'd3s-user-list',
    providers: [GridDefinitionService, UriBasedService, PermissionsService, ResourcesService, CompanySettingsService],
    template: `
        <header *ngIf="!showEditor && !showDelete && !showResetPwd">Users
            <d3s-tile-actions [hasAdd]="hasModifyAssetPermissions()" (addClick)="add()" hasFilterMode="true"
                              [(filterMode)]="showSimpleFilter" hasExport="true"
                              (exportClick)="export()"></d3s-tile-actions>
        </header>
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <span *ngIf="!showDelete && !showEditor && !showResetPwd">
                    <input type="text" [hidden]="!showSimpleFilter" pInputText size="100"
                           (input)="$event.target.value;dt.filterGlobal($event.target.value, 'contains')"
                           placeholder="Search..." class="grid-simple-filter">
                    <p-table #dt [loading]="isLoading" loadingIcon="fa fa-spinner" [scrollable]="true"
                             scrollWidth="100%" [lazy]="true" (onLazyLoad)="lazyLoadUsers($event)"
                             [totalRecords]="totalRecords" [value]="items" selectionMode="single"
                             [metaKeySelection]="true" [globalFilterFields]="globalFilterFields" [pageLinks]="3"
                             [paginator]="true" [rows]="rowsPerPage" [rowsPerPageOptions]="defaultPagingOptions"
                             [(selection)]="selected">
                        <ng-template pTemplate="header">
                            <tr>

                                <th *ngFor="let column of columns"
                                    [pSortableColumn]="column.sortable ? column.datafield : null">
                                    {{column.text}}
                                    <d3s-sortIcon *ngIf="column.sortable" [field]="column.datafield"></d3s-sortIcon>
                                </th>
                                <th style="width: 40px" *ngIf="hasModifyAssetPermissions()"></th>
                                <th style="width: 40px" *ngIf="hasDeleteAssetPermissions()"></th>
                                <th style="width: 40px" *ngIf="hasModifyAssetPermissions() && allowPasswordReset "></th>
                            </tr>
                            <tr [hidden]="showSimpleFilter">
                                <th *ngFor="let column of columns"><d3s-column-filter
                                        [field]="column.datafield"></d3s-column-filter></th>
                                <th *ngIf="hasModifyAssetPermissions()"></th>
                                <th *ngIf="hasDeleteAssetPermissions()"></th>
                                <th *ngIf="hasModifyAssetPermissions() && allowPasswordReset "></th>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="body" let-item>
                            <tr [pSelectableRow]="item">

                                <td *ngFor="let column of columns">
                                    <a *ngIf="column.datafield=='FirstName'"
                                       (click)="openResource(item)">{{item.FirstName}}</a>
                                    <d3s-dynamic-field-value *ngIf="column.datafield!='FirstName'" [column]="column"
                                                             [fields]="fields" [item]="item"></d3s-dynamic-field-value>                                                                 
                                </td>
                                <td *ngIf="hasModifyAssetPermissions()" style="width: 40px">
                                    <div class="RowTools" *ngIf="item.ResourceID > 0">
                                        <a style="cursor:pointer;" (click)="selected=item;showEditor=true;"><i
                                                class="fa fa-pencil"></i></a>
                                    </div>
                                </td>
                                <td *ngIf="hasDeleteAssetPermissions()" style="width: 40px">
                                    <div class="RowTools" *ngIf="item.ResourceID > 0">
                                        <a style="cursor:pointer;" (click)="selected=item;showDelete=true;"><i
                                                class="fa fa-trash-o"></i></a>
                                    </div>
                                </td>
                                <td *ngIf="hasModifyAssetPermissions() && allowPasswordReset " style="width: 40px">
                                    <div class="RowTools" *ngIf="item.ID>0">
                                        <a title="Reset Password" style="cursor:pointer;"
                                           (click)="selected=item;showResetPwd=true;"><i
                                                class="fa fa-asterisk fa-fw"></i></a>
                                    </div>
                                </td>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="summary">
                            <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows"
                                                  [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                        </ng-template>
                    </p-table>
                </span>
        <span *ngIf="showResetPwd">
                    <header>Reset Users Password</header>
                    <div class="row">
                        <div class="col s12">Are you sure you would like to reset the password for [{{selected.FirstName}} {{selected.LastName}}
                            ]</div>
                        <div class="col s12">&nbsp;</div>
                        <div class="col s12">
                            <button pButton type="button" (click)="resetPassword()" label="Reset Password"
                                    style="width: 150px;"></button>                            
                            <button pButton type="button" (click)="showResetPwd=false" label="Cancel"
                                    style="width: 150px;"></button>
                        </div>
                    </div>
                </span>
        <d3s-dynamic-editor *ngIf="showEditor" [objectID]="objectID" objectType="ResourceType" title="Resource"
                            [selection]="selected" rowID="ResourceID" (saveClick)="saveItem($event)"
                            (closeClick)="closeEditor()"></d3s-dynamic-editor>
        <d3s-delete-form *ngIf="showDelete"
                         [callback]="theDeleteCallback"
                         [itemId]="selected?.ID"
                         method="callback"
                         [prompt]="'Are you sure you want to delete the user [' + selected.FirstName + ' ' + selected.LastName + ']?'"
                         (onCancel)="showDelete=false;"
        ></d3s-delete-form>
    `,
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class UserListComponent extends BaseComponent implements OnInit, OnDestroy {


    error: any;
    items: any[] = [];
    columns: GridColumn[] = [];
    fields: GridField[] = [];

    showDelete: boolean = false;
    showEditor: boolean = false;
    showResetPwd: boolean = false;

    allowPasswordReset: boolean = false;

    selected: any = null;
    simpleFilter: string = "";

    totalRecords: number;
    rowsPerPage: number = 10;
    private usersSub: ISubscription
    currentPageNumber: number = 0;
    sortField: string = undefined;
    sortOrder: SortOrder = SortOrder.None;
    filters: GridFilterExpression[] = [];

    get globalFilterFields(): string[] {
        let f = this.columns.map(c => c.datafield);
        return f;
    }


    theDeleteCallback: Function;

    @ViewChild('dt') datatable;

    constructor(private route: ActivatedRoute,
                private router: Router,
                protected uriBasedService: UriBasedService,
                private gridDefinitionService: GridDefinitionService,
                protected messagesService: MessagesService,
                private permissionsService: PermissionsService,
                private resourcesService: ResourcesService,
                private companySettingsService: CompanySettingsService,
                protected titleService: Title,
                protected headerBreadcrumbService: HeaderBreadcrumbService,
                private changeDetectorRef: ChangeDetectorRef) {
        super();
        this.setObjectInfo('ResourceType', 1);
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Resources');

        this.theDeleteCallback = this.deleteUser.bind(this);
        this.load();
    }

    ngOnDestroy(): void {
        if (this.usersSub) {
            this.usersSub.unsubscribe();
        }
    }

    private openResource(event) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('resource', event.ResourceID));
    }

    public export() {
        this.resourcesService.exportResources(this.objectID, this.sortOrder, this.sortField, this.simpleFilter, this.filters);
    }

    load() {
        this.loadPermissions(this.permissionsService, this.objectType, this.objectID);
        this.getFieldsDefinition();


        this.companySettingsService.getAuthenticationModel().then(res => {
            if (res.model == 'forms') {
                this.allowPasswordReset = true;
            }
        });
    }

    deleteUser(id: number) {
        this.uriBasedService.deleteItemWithResult('form/DeleteResourceByID?id=', id).then(res => {
            this.showMessageForResult(this.messagesService, res);
            this.showDelete = false;
            if (res.type != 'error') {
                this.items = this.items.filter(x => x.ID != id);
                this.changeDetectorRef.markForCheck();
            }
        });
    }

    getFieldsDefinition() {
        this.gridDefinitionService.getGridDefinition(this.objectID, this.objectType).subscribe(
            result => {
                this.columns = result.Columns;
                this.fields = result.Fields;

                this.getData();
            }
        );
    }

    getData() {
        this.isLoading = true;

        this.usersSub = this.resourcesService.getResourceLazy(this.objectID, this.currentPageNumber, this.rowsPerPage, this.sortOrder, this.sortField, this.simpleFilter, this.filters).pipe(
            debounceTime(3000))
            .subscribe(result => {
                this.items = result.results;
                this.totalRecords = result.total;
                if (this.items && this.items.length > 0) this.selected = this.items[0];
                this.isLoading = false;
                this.changeDetectorRef.markForCheck();
            });
    }

    public lazyLoadUsers(event: LazyLoadEvent) {
        //event.first = First row offset
        //event.rows = Number of rows per page
        //event.sortField = Field name to sort with
        //event.sortOrder = Sort order as number, 1 for asc and -1 for dec
        //filters: FilterMetadata object having field as key and filter value, filter matchMode as value      

        this.filters.splice(0, this.filters.length);
        this.simpleFilter = "";
        for (var key in event.filters) {
            var filter = event.filters[key];
            if (key == "global" && this.showSimpleFilter) {
                this.simpleFilter = filter.value;
                this.filters.splice(0, this.filters.length);
                break;
            } else if (key == "global") {
                continue;
            }

            var gridFilter = new GridFilterExpression();
            gridFilter.condition = "CONTAINS"
            gridFilter.field = key;
            gridFilter.value = filter.value;
            this.filters.push(gridFilter);
        }

        this.sortOrder = event.sortOrder;
        this.sortField = event.sortField == undefined ? "" : event.sortField;
        this.rowsPerPage = event.rows;
        this.currentPageNumber = event.first / event.rows;
        this.getData();
    }

    closeEditor() {
        this.showEditor = false;
    }

    add() {
        this.selected = null;
        this.showEditor = true;
    }

    saveItem(event) {
        this.isLoading = true;
        this.uriBasedService.saveItem('form/dynamicedit/create/resource/', 'form/dynamicedit/edit/resource/', event.item)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                this.showEditor = false;
                this.getData();
            });
    }

    resetPassword() {
        if (!this.selected.ID) {
            this.messagesService.showError("No User Selected", "Select a user to reset there password");
        }
        this.resourcesService.resetResourcesPassword(this.selected.ID).then(result => {
            this.showMessageForResult(this.messagesService, result);
            this.showResetPwd = false;
        });

    }
};
