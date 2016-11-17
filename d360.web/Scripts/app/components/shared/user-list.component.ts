import { Input, Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService, UriBasedService, GridDefinitionService, MessagesService, PermissionsService, ResourcesService} from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { GridDefinition, GridColumn, GridField } from '../../models/grid-definition.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

@Component({
    selector: 'd3s-user-list',    
    providers: [GridDefinitionService, UriBasedService, PermissionsService, ResourcesService],
    template: `                                         
                <header *ngIf="!showEditor && !showDelete && !showResetPwd">Users
                    <d3s-tile-actions [hasAdd]="hasRootCreatePermissions()" (addClick)="add()" hasFilterMode="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
                </header>                           
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <span *ngIf="!isLoading && !showDelete && !showEditor && !showResetPwd">
                    <input [hidden]="!showSimpleFilter" #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">                                              
                    <p-dataTable #dt [globalFilter]="gb" [value]="items" selectionMode="single" [rows]="defaultInitialItemsPerPage" paginator="true" pageLinks="3" [(selection)]="selected" [rowsPerPageOptions]="defaultPagingOptions">                                                                       
                        <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                        <p-column field="FirstName" header="First Name" sortable="true">
                            <template let-item="rowData" pTemplate type="body">
                                <a (click)="openResource(item)">{{item.FirstName}}</a>
                            </template>
                        </p-column>
                        <p-column *ngFor="let column of columns" [field]="column.datafield" [header]="column.text" [sortable]="column.sortable" [filter]="!showSimpleFilter">
                            <template let-item="rowData" pTemplate type="body">
                                <d3s-dynamic-field-value [column]="column" [fields]="fields" [item]="item"></d3s-dynamic-field-value>                                                                 
                            </template>
                        </p-column>
                        <p-column [style]="{width:'40px'}" *ngIf="hasRootUpdatePermissions()">
                            <template let-item="rowData" pTemplate type="body">
                                <div class="RowTools">
                                    <a style="cursor:pointer;" (click)="selected=item;showEditor=true;"><i class="fa fa-pencil"></i></a>                                        
                                </div>
                            </template>
                        </p-column>                            
                        <p-column  [style]="{width:'40px'}" *ngIf="hasRootDeletePermissions()">
                               <template let-item="rowData" pTemplate type="body">
                                <div class="RowTools">                                
                                    <a style="cursor:pointer;" (click)="selected=item;showDelete=true;"><i class="fa fa-trash-o"></i></a>                                    
                                </div>
                               </template>
                        </p-column>                            
                            <p-column  [style]="{width:'40px'}" *ngIf="hasRootCreatePermissions() && allowPasswordReset ">
                                <template let-item="rowData" pTemplate type="body">
                                    <div class="RowTools" *ngIf="item.ID>0">                                
                                        <a title="Reset Password" style="cursor:pointer;" (click)="selected=item;showResetPwd=true;"><i class="fa fa-asterisk fa-fw"></i></a>                                    
                                    </div>
                                </template>
                            </p-column>     
                    </p-dataTable>
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
                <delete-form *ngIf="showDelete"
                                [callback]="theDeleteCallback"
                                [itemId]="selected?.ID"
                                method="callback"
                                [prompt]="'Are you sure you want to delete the user [' + selected.FirstName + ' ' + selected.LastName + ']?'"                                         
                                (onCancel)="showDelete=false;"
                ></delete-form>
                `
})

export class UserListComponent extends BaseComponent{    
    private objectID: number = 1;
    private objectType: string = 'ResourceType';
    error: any;
    items: any[] = [];
    columns: GridColumn[] = [];
    fields: GridField[] = [];

    showDelete: boolean = false;
    showEditor: boolean = false;
    showResetPwd: boolean = false;

    allowPasswordReset: boolean = false;

    selected: any = null;
    

    theDeleteCallback: Function;

    constructor(private route: ActivatedRoute,
        private router: Router,
        protected uriBasedService: UriBasedService,
        private gridDefinitionService: GridDefinitionService, 
        protected messagesService: MessagesService,
        private permissionsService: PermissionsService,
        private resourcesService: ResourcesService,
        protected titleService: Title, protected headerBreadcrumbService: HeaderBreadcrumbService) {
        super();
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

    load() {
        this.loadPermissions(this.permissionsService, this.objectType, this.objectID);
        this.getFieldsDefinition();
        this.getData();

        this.resourcesService.getAuthenticationModel().then(res => {            
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