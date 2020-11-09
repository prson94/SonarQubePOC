import { debounceTime } from 'rxjs/operators';
import { Title } from '@angular/platform-browser';
import { Router } from '@angular/router';
import { GridColumn, GridField, GridFilterExpression, GridFilterColumn } from '../../../models/grid-definition.model';
import { GridDefinitionService } from '../../../services/grid-definition.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { PermissionsService } from '../../../services/permissions.service';
import { ResourcesService } from '../../../services/resources.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { BaseComponent } from '../../shared/base.component';
import { LazyLoadEvent } from 'primeng/api';
import { SubscriptionLike as ISubscription } from 'rxjs';
import { SortOrder } from '../../../models/enums.model';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, ViewChild, OnInit } from '@angular/core';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { V2ApiFilters } from '../../../models/asset-search.model';
import { ResourceApiModel } from '../../../models/resource.model';

@Component({
    selector: 'd3s-user-list',
    providers: [GridDefinitionService, PermissionsService, ResourcesService, CompanySettingsService],
    templateUrl: 'user-list.component.html',
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

    @ViewChild('dt', { static: false }) datatable;

    constructor(
        private router: Router,        
        private gridDefinitionService: GridDefinitionService,
        protected messagesService: MessagesObservableService,
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

    public openResource(event) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('resource', event.ResourceID));
    }

    public export() {
        this.resourcesService.exportResources(this.getParams());
    }

    load() {
        this.loadPermissions(this.permissionsService, this.objectType, this.objectID);
        this.getFieldsDefinition();


        this.companySettingsService.getAuthenticationModel().subscribe(res => {
            if (res.model == 'forms') {
                this.allowPasswordReset = true;
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
        if (this.usersSub)
            this.usersSub.unsubscribe();

        this.usersSub = this.resourcesService.getResourceLazy(this.getParams()).pipe(
            debounceTime(3000))
            .subscribe(result => {
                if (result) {
                    this.items = result.items;
                    this.totalRecords = result.total;
                    if (this.items && this.items.length > 0) this.selected = this.items[0];
                    this.isLoading = false;
                    this.changeDetectorRef.markForCheck();
                }
            },
                (err) => {
                    this.items = [];
                    this.totalRecords = 0;
                    this.isLoading = false;
                    this.changeDetectorRef.markForCheck();
                },
                () => {
                    this.isLoading = false;
                    this.changeDetectorRef.markForCheck();
                }
            );
    }

    public getParams() {
        var params = new V2ApiFilters();
        let baseFilter = `(State eq 'Active' or State eq 'Inactive')`;

        params._direction = this.sortOrder == 1 ? 'asc' : 'desc';
        if (this.sortField) {
            params._order = this.getApiName(this.sortField);
        }
        else {
            params._order = "FirstName";
        }

        if (this.simpleFilter) {
            params._simpleFilter = this.simpleFilter;
        }
        else {
            delete params['_simpleFilter'];
        }

        if (this.filters.length > 0) {
            let expressions: string[] = [];
            let filterColumns: GridFilterColumn[] = [];
            this.columns.forEach(f => {
                var gfc = new GridFilterColumn();
                gfc.apiName = this.getApiName(f.datafield);
                gfc.fieldType = f['fieldType'];
                gfc.datafield = f.datafield;
                filterColumns.push(gfc);
            });
            this.filters.forEach(f => {
                expressions.push(f.getAsV2ApiFilter(filterColumns));
            });

            if (expressions.length > 0) {
                params._filter = `(${expressions.join(' and ')}) and ${baseFilter}`;
            }
            else {
                params._filter = baseFilter;
            }
        }
        else {
            params._filter = baseFilter;
        }

        params._pageNum = this.currentPageNumber + 1;
        params._pageSize = this.rowsPerPage;

        return params;
    }

    getApiName(fieldName: string): string {
        return this.fields.find(x => x.name == fieldName).apiName;
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

    saveUser(event) {        
        const user = new ResourceApiModel;
                
        user.FirstName = event.item.FirstName;
        user.LastName = event.item.LastName;
        user.IsAdministrator = event.item.IsAdministrator;
        user.Username = event.item.Email;
        
        if (event.item.ID > 0) {
            user.uid = this.selected.uid;
            user.State = event.item.State;
        }
        else {
            user.Password = event.item.Password;
        }

        user.Fields = new Object();

        // handle dynamic fields
        for (let key in event.item) {
            if (key != 'Email' && key != 'FirstName' && key != 'LastName' && key != 'IsAdministrator' && key != 'State' && key != 'ID' && key != 'Password') {                
                user.Fields[key] = event.item[key];                
            }
        }

        this.isLoading = true;
        this.resourcesService.saveResource(user, true, false)
            .subscribe(
                result => {
                    if (result.Success) {
                        result.Message = null;
                        this.showEditor = false;
                        this.getData();
                    }
                    else {
                        this.isLoading = false;
                        this.changeDetectorRef.markForCheck();
                    }
                    this.showMessageForApiResult(this.messagesService, result, `User(s) successfully ${event.item.ID > 0 ? 'updated' : 'added'}`);
                }
            )        
    }


    deleteUser(id: number) {
        this.resourcesService.deleteResource(this.selected.uid)
            .subscribe(
                result => {
                    this.showMessageForResult(this.messagesService, result, 'User successfully deleted');
                    this.showDelete = false;
                    if (result.type !== 'error') {
                        this.items = this.items.filter(x => x.ID !== id);
                    }
                    this.changeDetectorRef.markForCheck();
                }
            )
    }

    resetPassword() {
        if (!this.selected.ResourceID) {
            this.messagesService.showError("No User Selected", "Select a user to reset there password");
        }
        this.resourcesService.resetResourcesPassword(this.selected.ResourceID).subscribe(result => {
            this.showMessageForResult(this.messagesService, result);
            this.showResetPwd = false;
        });

    }
};
