import { Input, Component, OnInit, ViewChild } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { UriBasedService } from '../../services/uri-based.service';
import { GridDefinitionService } from '../../services/grid-definition.service';
import { MessagesService } from '../../services/messages.service';
import { PermissionsService } from '../../services/permissions.service';
import { ResourcesService } from '../../services/resources.service';
import { CompanySettingsService } from '../../services/settings.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { GridDefinition, GridColumn, GridField } from '../../models/grid-definition.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

@Component({
    selector: 'd3s-user-list',    
    providers: [GridDefinitionService, UriBasedService, PermissionsService, ResourcesService, CompanySettingsService],
    template: `                                         
                <header *ngIf="!showEditor && !showDelete && !showResetPwd">Users
                    <d3s-tile-actions [hasAdd]="hasModifyAssetPermissions()" (addClick)="add()" hasFilterMode="true" [(filterMode)]="showSimpleFilter" hasExport="true" (exportClick)="export()"></d3s-tile-actions>                            
                </header>                           
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <span *ngIf="!isLoading && !showDelete && !showEditor && !showResetPwd">
                    <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..." class="grid-simple-filter">
                    <p-table #dt [value]="items" selectionMode="single" [metaKeySelection]="true" [globalFilterFields]="globalFilterFields" [pageLinks]="3" [paginator]="true" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions" [(selection)]="selected">
                        <ng-template pTemplate="header">
                            <tr>
                                <th [pSortableColumn]="'FirstName'">
                                    First Name
                                    <d3s-sortIcon [field]="'FirstName'"></d3s-sortIcon>
                                </th>
                                <th *ngFor="let column of columns" [pSortableColumn]="column.sortable ? column.datafield : null">
                                    {{column.text}}
                                    <d3s-sortIcon *ngIf="column.sortable" [field]="column.datafield"></d3s-sortIcon>
                                </th>
                                <th style="width: 40px" *ngIf="hasModifyAssetPermissions()"></th>
                                <th style="width: 40px" *ngIf="hasDeleteAssetPermissions()"></th>
                                <th style="width: 40px" *ngIf="hasModifyAssetPermissions() && allowPasswordReset "></th>
                            </tr>
                            <tr [hidden]="showSimpleFilter">
                                <th><d3s-column-filter [field]="'FirstName'" [datatype]="'text'"></d3s-column-filter></th>
                                <th *ngFor="let column of columns"><d3s-column-filter [field]="column.datafield"></d3s-column-filter></th>
                                <th *ngIf="hasModifyAssetPermissions()"></th>
                                <th *ngIf="hasDeleteAssetPermissions()"></th>
                                <th *ngIf="hasModifyAssetPermissions() && allowPasswordReset "></th>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="body" let-item>
                            <tr [pSelectableRow]="item">
                                <td>
                                    <a (click)="openResource(item)">{{item.FirstName}}</a>
                                </td>
                                <td *ngFor="let column of columns">
                                    <d3s-dynamic-field-value [column]="column" [fields]="fields" [item]="item"></d3s-dynamic-field-value>                                                                 
                                </td>
                                <td *ngIf="hasModifyAssetPermissions()">
                                    <div class="RowTools" *ngIf="item.ResourceID > 0">
                                        <a style="cursor:pointer;" (click)="selected=item;showEditor=true;"><i class="fa fa-pencil"></i></a>
                                    </div>
                                </td>
                                <td *ngIf="hasDeleteAssetPermissions()">
                                    <div class="RowTools" *ngIf="item.ResourceID > 0">
                                        <a style="cursor:pointer;" (click)="selected=item;showDelete=true;"><i class="fa fa-trash-o"></i></a>
                                    </div>
                                </td>
                                <td *ngIf="hasModifyAssetPermissions() && allowPasswordReset ">
                                    <div class="RowTools" *ngIf="item.ID>0">
                                        <a title="Reset Password" style="cursor:pointer;" (click)="selected=item;showResetPwd=true;"><i class="fa fa-asterisk fa-fw"></i></a>
                                    </div>
                                </td>
                            </tr>
                        </ng-template>
                        <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                            <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                        </ng-template>
                    </p-table>
                </span>
                <span *ngIf="showResetPwd">
                    <header>Reset Users Password</header>
                    <div class="row">
                        <div class="col s12">Are you sure you would like to reset the password for [{{selected.FirstName}} {{selected.LastName}}]</div>
                        <div class="col s12">&nbsp;</div>
                        <div class="col s12">
                            <button pButton type="button" (click)="resetPassword()" label="Reset Password" style="width: 150px;"></button>                            
                            <button pButton type="button" (click)="showResetPwd=false" label="Cancel" style="width: 150px;"></button>
                        </div>
                    </div>
                </span>
                <d3s-dynamic-editor *ngIf="showEditor" [objectID]="objectID" objectType="ResourceType" title="Resource" [selection]="selected" rowID="ResourceID" (saveClick)="saveItem($event)" (closeClick)="closeEditor()"></d3s-dynamic-editor>
                <d3s-delete-form *ngIf="showDelete"
                                [callback]="theDeleteCallback"
                                [itemId]="selected?.ID"
                                method="callback"
                                [prompt]="'Are you sure you want to delete the user [' + selected.FirstName + ' ' + selected.LastName + ']?'"                                         
                                (onCancel)="showDelete=false;"
                ></d3s-delete-form>
                `
})

export class UserListComponent extends BaseComponent{    
    
    error: any;
    items: any[] = [];
    columns: GridColumn[] = [];
    fields: GridField[] = [];

    showDelete: boolean = false;
    showEditor: boolean = false;
    showResetPwd: boolean = false;

    allowPasswordReset: boolean = false;

    selected: any = null;
    
    get globalFilterFields(): string[] {
        let f = this.columns.map(c => c.datafield);
        f.push('FirstName');
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
        protected titleService: Title, protected headerBreadcrumbService: HeaderBreadcrumbService) {
        super();
        this.setObjectInfo('ResourceType', 1);
    }    

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Resources');

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Resources'));
        this.theDeleteCallback = this.deleteUser.bind(this);
        this.load();
    }
    
    private openResource(event) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('resource',event.ResourceID));
    }

    public export() {
        this.resourcesService.exportResources(this.objectID);
    }

    load() {
        this.loadPermissions(this.permissionsService, this.objectType, this.objectID);
        this.getFieldsDefinition();
        this.getData();

        this.companySettingsService.getAuthenticationModel().then(res => {            
            if (res.model == 'forms') {
                this.allowPasswordReset = true;
            }
        });
    }

    deleteUser(id: number) {
        this.uriBasedService.deleteItemWithResult('form/DeleteResourceByID?id=', id).
            then(res => {
                this.showMessageForResult(this.messagesService, res);
                this.showDelete = false;
                if (res.type != 'error')
                    this.items = this.items.filter(x => x.ID != id);
            });
    }

    getFieldsDefinition() {
        this.gridDefinitionService.getGridDefinition(this.objectID, this.objectType)
            .then(result => {
                this.columns = result.Columns.filter(x => x.datafield != 'FirstName');
                this.fields = result.Fields;
            });
    }

    getData() {
        this.isLoading = true;
        this.uriBasedService.getItems(`/api/resources/${this.objectID}?$orderby=LastName,FirstName`)
            .then(result => {
                this.items = result;
                this.isLoading = false;
                if (this.items.length > 0) this.selected = this.items[0];
            });
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