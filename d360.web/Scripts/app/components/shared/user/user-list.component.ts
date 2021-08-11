import { debounceTime } from 'rxjs/operators';
import { Title } from '@angular/platform-browser';
import { Router } from '@angular/router';
import { GridColumn, GridField } from '../../../models/grid-definition.model';
import { GridDefinitionService } from '../../../services/grid-definition.service';
import { FieldsObservableService } from "../../../services/fieldsObservable.service";
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { PermissionsService } from '../../../services/permissions.service';
import { ResourcesService } from '../../../services/resources.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { BaseComponent } from '../../shared/base.component';
import { LazyLoadEvent } from 'primeng/api';
import { forkJoin, Observable, ReplaySubject, SubscriptionLike as ISubscription } from 'rxjs';
import { SortOrder } from '../../../models/enums.model';
import { Input, Output, EventEmitter, ChangeDetectionStrategy, ChangeDetectorRef, Component, OnChanges, SimpleChange, OnDestroy, ViewChild, OnInit } from '@angular/core';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { V2ApiFilters } from '../../../models/asset-search.model';
import { ResourceApiModel } from '../../../models/resource.model';
import { FieldType, FieldTypeAPIModelField } from "../../../models/fieldtype-api.model";
import { AdvancedFilterFieldType, Filters } from "../../assets-grid/advanced-filtering/advanced-filtering.models";
import { isEqual } from "lodash";

@Component({
    selector: "d3s-user-list",
    providers: [GridDefinitionService, FieldsObservableService, PermissionsService, ResourcesService, CompanySettingsService],
    templateUrl: "user-list.component.html",
    styleUrls: ["user-list.component.less"],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class UserListComponent extends BaseComponent implements OnInit, OnDestroy {
    @Input() ResponsibilityTypeUid: string;
    @Input() IsCommunityUserResposibility: boolean = false;
    @Input() UserListHeading: string = 'Users';
    @Input() selected: any = null;
    @Output() selectedChange = new EventEmitter();

    error: any;
    items: any[] = [];
    columns: GridColumn[] = [];
    fields: GridField[] = [];

    filterFields$: Observable<AdvancedFilterFieldType[]>;
    private filterFieldsSubject: ReplaySubject<AdvancedFilterFieldType[]> = new ReplaySubject(1);

    showDelete: boolean = false;
    showEditor: boolean = false;
    showResetPwd: boolean = false;

    allowPasswordReset: boolean = false;
    isExportInProgress: boolean = false;
    simpleFilter: string = "";
    advancedFilter: string = "";

    totalRecords: number;
    rowsPerPage: number = 10;
    private usersSub: ISubscription
    currentPageNumber: number = 0;
    sortField: string = undefined;
    sortOrder: SortOrder = SortOrder.None;
    previousEvent: LazyLoadEvent;
    columnWidth: number = 0;
    columnWidthOwnedItems: number = 0;

    get globalFilterFields(): string[] {
        let f = this.columns.map(c => c.datafield);
        return f;
    }

    theDeleteCallback: Function;

    @ViewChild('dt', { static: false }) datatable;

    constructor(
        private router: Router,        
        private gridDefinitionService: GridDefinitionService,
        private fieldsService: FieldsObservableService,
        protected messagesService: MessagesObservableService,
        private permissionsService: PermissionsService,
        private resourcesService: ResourcesService,
        private companySettingsService: CompanySettingsService,
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        private changeDetectorRef: ChangeDetectorRef) {
        super();
        this.setObjectInfo('ResourceType', 1);
        this.filterFields$ = this.filterFieldsSubject.asObservable();
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Resources');

        this.theDeleteCallback = this.deleteUser.bind(this);
        this.load();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes["ResponsibilityTypeUid"] && "" + this.ResponsibilityTypeUid !== "") {
            this.getData();
        }
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
        var filename = this.IsCommunityUserResposibility === true ? `Filtered List of ${this.UserListHeading} ${new Date().toDateString()}.xlsx` : "Users.xlsx";
        this.isExportInProgress = true;
        this.resourcesService.exportResources(this.getParams(), filename).subscribe(
            (res) => {
                this.isExportInProgress = false;
                this.changeDetectorRef.markForCheck();
            }
        );
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

    getLoadIdentifier() {
        return "User" + (this.IsCommunityUserResposibility ? "Community" : "");
    }

    getFieldsDefinition() {
        let params = { IsCommunityUserResposibility: this.IsCommunityUserResposibility };

        forkJoin(
            this.gridDefinitionService.getGridDefinition(this.objectID, this.objectType, null, null, params),
            this.fieldsService.getFieldsV2(this.resourceTypeUid, null, null)
        ).subscribe((forkResult) => {
            const result = forkResult[0];
            const customFields = forkResult[1];

            this.columns = result.Columns;
            this.fields = result.Fields;
            if (this.IsCommunityUserResposibility && this.columns && this.columns.length > 2) {
                this.columnWidth = 200;
                this.columnWidthOwnedItems = 120;
            }
            else {
                this.columnWidth = 0;
                this.columnWidthOwnedItems = 0;
            }
            this.setAdvancedFilterFields(result.Columns, customFields);
        });

    }

    setAdvancedFilterFields(columns: GridColumn[], customFields: FieldTypeAPIModelField[]) {
        let output: AdvancedFilterFieldType[] = columns.map((c) => {
            if (c.datafield === "State") {
                return {
                    Name: this.getApiName(c.datafield),
                    FriendlyName: c.text,
                    Type: new FieldType("Lookup"),
                    Category: "",
                    ValueList: [{ value: "Active", title: "Active" }, { value: "Inactive", title: "Inactive" }],
                    RemovePopulatedOperator: true
                }
            } else {
                return {
                    Name: this.getApiName(c.datafield),
                    FriendlyName: c.text,
                    Type: new FieldType(c.fieldType),
                    Category: "",
                    RemovePopulatedOperator: ["FirstName", "LastName", "Email"].indexOf(c.datafield) !== -1
                }
            }
        });

        customFields.forEach((c) => {
            if (!this.IsCommunityUserResposibility && output.findIndex((o) => o.Name === c.Name) === -1) {
                output.push(c as AdvancedFilterFieldType);
            }
        });

        this.filterFieldsSubject.next(output);
        this.filterFieldsSubject.complete();
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

        if (this.advancedFilter.length > 0) {
            params._filter = `(${this.advancedFilter}) and ${baseFilter}`;
        } else {
            params._filter = baseFilter;
        }

        if (this.IsCommunityUserResposibility) {
            params['IsCommunityUserResposibility'] = this.IsCommunityUserResposibility;
            params['ResponsibilityTypeUid'] = this.ResponsibilityTypeUid;
        }
        else {
            params['IsCommunityUserResposibility'] = this.IsCommunityUserResposibility;
        }

        params._pageNum = this.currentPageNumber + 1;
        params._pageSize = this.rowsPerPage;

        return params;
    }

    getApiName(fieldName: string): string {
        return this.fields.find(x => x.name == fieldName).apiName;
    }

    public lazyLoadUsers(event: LazyLoadEvent) {
        //if its the same filter then no need to load same data 
        if (isEqual(event, this.previousEvent)) {
            return;
        }
        this.previousEvent = event;
        //event.first = First row offset
        //event.rows = Number of rows per page
        //event.sortField = Field name to sort with
        //event.sortOrder = Sort order as number, 1 for asc and -1 for dec
        //filters: FilterMetadata object having field as key and filter value, filter matchMode as value      

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

    canExportRecords() {
        if (this.IsCommunityUserResposibility) {
            return this.totalRecords <= this.maxExportRows;
        }
        else {
            return true;
        }
    }

    IsReadOnly() {
        return !this.IsCommunityUserResposibility;
    }

    setStyleWidth(datafield: string) {
        if (this.columnWidth > 0 && this.columnWidthOwnedItems > 0) {
            if (datafield === "OwnedItemCount") {
                return this.columnWidthOwnedItems + 'px';
            }
            else {
                return this.columnWidth + 'px';
            }
        }
        else {
            return null;
        }
    }

    advancedFiltersChanged($event: Filters) {
        this.advancedFilter = $event.filter;
        this.getData();
    }
    onFiltersLoaded() {
        this.getData();
    }
};
