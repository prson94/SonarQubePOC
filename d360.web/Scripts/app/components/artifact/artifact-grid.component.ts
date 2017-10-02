import { Component, Input, Output, OnChanges, SimpleChange, EventEmitter, OnInit } from '@angular/core';
import { LazyLoadEvent, DataTable } from 'primeng/primeng';
import { Lookup, LookupItem } from '../../models/lookup.model';
import { GridDefinition, GridColumn, GridField, GridFilterColumn, GridFilterExpression, GridRelationshipFilterExpression, GridAttributeFilterExpression } from '../../models/grid-definition.model';
import { MessagesService } from '../../services/messages.service';
import { GridDefinitionService } from '../../services/grid-definition.service';
import { ArtifactService } from '../../services/artifacts.service';
import { PermissionsService } from '../../services/permissions.service';
import { StateService } from '../../services/state.service';
import { HeaderActionsService } from '../../services/header-actions.service';
import { ArtifactType } from '../../models/artifact-type.model';
import { Router, ActivatedRoute }       from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { StringConstants } from '../../static/string-constants';

@Component({
    selector: 'd3s-artifact-grid',
    providers: [GridDefinitionService, ArtifactService, PermissionsService],
    template: ` <header *ngIf="!showEditor && !showDelete">
                    <i *ngIf="artifactType && artifactType.Description" class="fa" [ngClass]="{'fa-plus-square-o':!showArtifactDetails,'fa-minus-square-o':showArtifactDetails}" aria-hidden="true" style="padding-right:5px;cursor:pointer;font-size:12px" title="Details" (click)="toggleArtifactDetail()" (mouseenter)="showArtifactDetails=true"></i>
                    {{artifactType?.Name}}
                    {{titlePostfix}}
                    <d3s-tile-actions [hasAdd]="showAddButton && hasRootCreatePermissions()" [hasExport]="!artifactType.HasCustomExportTemplates" [hasCustomExport]="artifactType.HasCustomExportTemplates" (addClick)="add()" (customExportClick)="customExport()" (exportClick)="export(false)" [hasFilterMode]="true" [filterMode]="showGridSimpleFilter" (filterModeChange)="resetFilters($event);"></d3s-tile-actions>
                    <div (click)="toggleArtifactDetail()" *ngIf="showArtifactDetails && artifactType && artifactType.Description" [innerHtml]="artifactType.Description" style="text-transform:none;font-size:12px;font-weight:normal;margin-left:20px"></div>
                </header>    
                <d3s-loading [isLoading]="isLoading"></d3s-loading>                
                <div class="row" *ngIf="!isLoading && !showDelete && !showEditor">                    
                    <div #rightMenu [ngClass]="{'artifact-context-menu':isMenuOpen,'artifact-context-menu-closed':!isMenuOpen}">
                        <a [href]="itemUrl" target="_blank">Open in new window</a>
                    </div>
                    <div class="col s12" *ngIf="showGridSimpleFilter">                                                
                        <input type="text" *ngIf="topLevelFilters.length ==0" pInputText style="width: 100%;" maxlength="200" (keyup)="checkSimpleSearchEnter($event,dt);" [(ngModel)]="stateService.artifactTypeFilters.simpleTextFilter" placeholder="Search..." autofocus autocomplete="off" />                            
                        <d3s-artifact-top-level-filter *ngIf="topLevelFilters.length > 0" [(filters)]="stateService.artifactTypeFilters.filters" [fields]="topLevelFilters" (filterChanged)="filterGridData()"></d3s-artifact-top-level-filter>
                    </div>
                    <d3s-artifact-column-filter *ngIf="!showGridSimpleFilter" [(attributeFilters)]="stateService.artifactTypeFilters.attributes" [(ownerFilter)]="stateService.artifactTypeFilters.owners" [(relationshipFilters)]="stateService.artifactTypeFilters.relationships" [(filters)]="stateService.artifactTypeFilters.filters" [artifactType]="artifactType" [fields]="filtercolumns" (filterChanged)="filterGridData()"></d3s-artifact-column-filter>
                    <d3s-loading [isLoading]="isGridFilterLoading"></d3s-loading>
                    <d3s-artifact-custom-export *ngIf="showCustomExport" (closeClick)="showCustomExport=false" 
                            [artifactType]="artifactType" 
                            [sortOrder]="stateService.artifactTypeFilters.sortOrder" 
                            [sortField]="stateService.artifactTypeFilters.sortField"
                            [filters]="stateService.artifactTypeFilters.filters"
                            [relationships]="stateService.artifactTypeFilters.relationships"
                            [attributes]="stateService.artifactTypeFilters.attributes"
                            [simpleFilter]="stateService.artifactTypeFilters.simpleTextFilter"
                            [owner]="stateService.artifactTypeFilters.owners"
                    ></d3s-artifact-custom-export>
                    <div class="col s12" [hidden]="showCustomExport">                
                       <p-dataTable #dt lazy="true" [totalRecords]="totalRecords"  scrollable="true" scrollWidth="100%" [value]="items" selectionMode="single" [rows]="rowsPerPage" paginator="true" pageLinks="3" (onRowDblclick)="selectArtifact($event.data)" [(selection)]="selected" (onLazyLoad)="loadArtifactsLazy($event)" [rowsPerPageOptions]="defaultPagingOptions">
                            <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>
                            <p-column *ngFor="let column of columns" [field]="column.datafield" [header]="column.text" [sortable]="column.sortable" [style]="{width:column.columnWidth ? column.columnWidth + 'px' : ''}">                                                                
                                <ng-template let-item="rowData" pTemplate type="body">
                                    <a *ngIf="column.text == 'Name';else showField" (contextmenu)="onRightClick($event,rightMenu,item,dt)" (click)="selectArtifact(item)">{{item[column.datafield]}}</a>     
                                    <ng-template #showField>
                                        <d3s-dynamic-field-value [column]="column" [fields]="fields" [item]="item"></d3s-dynamic-field-value>                                 
                                    </ng-template>
                                </ng-template>
                            </p-column>
                            <p-column [style]="{width:'30px'}" *ngIf="showEditButton">
                                    <ng-template let-item="rowData" pTemplate type="body">
                                        <div class="RowTools" *ngIf="item.P_CanEdit">
                                            <a style="cursor:pointer;" (click)="selected=item;showEditor=true;"><i class="fa fa-pencil"></i></a>                                        
                                        </div>
                                    </ng-template>
                            </p-column>                            
                            <p-column  [style]="{width:'35px'}" *ngIf="showDeleteButton">
                                    <ng-template let-item="rowData" pTemplate type="body">
                                        <div class="RowTools" *ngIf="item.P_CanDelete">
                                            <a style="cursor:pointer;" (click)="selected=item;showDelete=true;"><i class="fa fa-trash-o"></i></a>                                    
                                        </div>
                                    </ng-template>
                            </p-column>                            
                            <p-column [style]="{width:'30px'}">
                                    <ng-template let-item="rowData" pTemplate type="body">
                                        <div class="RowTools">
                                            <d3s-tooltip objectType="Artifact" [objectId]="item.ID" (click)="selectArtifact(item)" tooltipType="Preview" icon="info"></d3s-tooltip>                                            
                                        </div>
                                    </ng-template>
                            </p-column>
                        </p-dataTable>                           
                    </div>
                </div>                                  
                <d3s-dynamic-editor *ngIf="showEditor" [newActionName]="New" [objectID]="artifactType?.ID" objectType="Artifact" [title]="artifactType?.Name + ' Item'" [selection]="selected" [rowID]="rowID" (saveClick)="saveItem($event)" (closeClick)="closeEditor()"></d3s-dynamic-editor>
                <d3s-delete-form *ngIf="showDelete"
                            [callback]="theDeleteCallback"
                            [itemId]="selected?.ID"
                            method="callback"
                            [prompt]="'Are you sure you want to delete ['+ (selected?.DisplayValue ? selected?.DisplayValue : 'Artifact') + ']?'"                                         
                            (onCancel)="showDelete=false;"
                ></d3s-delete-form>  
                `,    
    host: {
        '(document:click)': 'clickedOutside()',
    },    
})

export class ArtifactGridComponent extends BaseComponent implements OnChanges {    
    @Input() rowID: string = 'ID';
    @Input() artifactType: ArtifactType;
    @Input() titlePostfix: string = ''; // added to end of header title.
    @Input() rowsPerPage: number = 25;    
            
    showEditButton: boolean = true;
    showDeleteButton: boolean = true;
    showAddButton: boolean = true;
    showCustomExport: boolean = false;
    isGridFilterLoading: boolean = false;
    isMenuOpen: boolean = false;
    showArtifactDetails: boolean = false;
    
    totalRecords: number;
        
    searchValue: string = "";
    
    searchDelayMilliSeconds: number = 500;

    error: any;
    items: any[];
    columns: GridColumn[] = [];    
    fields: GridField[] = [];
    filtercolumns: GridFilterColumn[] = [];
    topLevelFilters: GridFilterColumn[] = [];
    
    showDelete: boolean = false;
    showEditor: boolean = false;
    isLoading: boolean = false;

    simpleSearchID: number = 0;

    selected: any = null;
    itemUrl: string;

    theDeleteCallback: Function;
        
    constructor(private headerActionsService: HeaderActionsService,
        private messagesService: MessagesService,
        private stateService: StateService,
        private permissionsService: PermissionsService,
        private router: Router,        
        private gridDefinitionService: GridDefinitionService, private artifactService: ArtifactService) {
        super();
        this.theDeleteCallback = this.deleteItem.bind(this);        
    }

    get showGridSimpleFilter(): boolean {        
        return this.stateService.artifactTypeFilters.showSimpleFilter;
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes['artifactType'] && this.artifactType != null) {            
            this.load();
        }

        //clear out the filters if the artifacttype is different
        this.stateService.resetArtifactTypeFilterIfRequired(this.artifactType.ID);        
    }

    load() {        
        this.loadPermissions(this.permissionsService, StringConstants.ObjectArtifactType, this.artifactType.ID);
        this.getFieldsDefinition();
        if (this.artifactType.AutoDisplayDescription) {
            this.toggleArtifactDetail();
        }
    }

    public filterGridData() {
        this.isGridFilterLoading = true;
        this.stateService.artifactTypeFilters.currentPageNumber = 0;
        this.getData();
    }

    resetFilters(val) {        
        this.stateService.artifactTypeFilters.showSimpleFilter = val;        
        this.stateService.artifactTypeFilters.simpleTextFilter = '';
        this.stateService.artifactTypeFilters.filters = [];
        this.stateService.artifactTypeFilters.attributes = [];
        this.stateService.artifactTypeFilters.relationships = [];
        this.stateService.artifactTypeFilters.owners = null;

        this.filterGridData();
    }

    deleteItem(id: number) {
        this.artifactService.deleteArtifact(id).
            then(result => {
                this.showMessageForResult(this.messagesService, result);
                this.headerActionsService.emitFavoritesChange(); // favorites need to be reloaded if an object was removed
                this.showDelete = false;                
                this.getData();
            });
    }

    getFieldsDefinition() {
        this.gridDefinitionService.getGridDefinition(this.artifactType.ID, StringConstants.ObjectArtifactType)
            .then(result => {
                this.columns = result.Columns.filter(x => x.datafield != 'Name');
                this.filtercolumns = result.FilterColumns;
                this.fields = result.Fields;
                this.topLevelFilters = result.TopLevelFilterColumns;
            });
    }
    
    getData() {        
        this.artifactService.getArtifacts(this.artifactType.ID, this.rowsPerPage, this.stateService.artifactTypeFilters.currentPageNumber, this.stateService.artifactTypeFilters.sortField, this.stateService.artifactTypeFilters.sortOrder, this.stateService.artifactTypeFilters.filters, this.stateService.artifactTypeFilters.relationships, this.stateService.artifactTypeFilters.attributes, this.stateService.artifactTypeFilters.simpleTextFilter, this.stateService.artifactTypeFilters.owners)
            .then(result => {
                this.items = result.results;
                this.totalRecords = result.total;                
                if (this.items && this.items.length > 0) this.selected = this.items[0]; 
                this.simpleSearchID = 0;
                this.isGridFilterLoading = false;              
            });
    }

    closeEditor() {
        this.showEditor = false;
    }

    add() {
        this.selected = null;
        this.showEditor = true;
    }

    export(listableOnly) {
        this.artifactService.getArtifactsXls(listableOnly, this.artifactType, this.stateService.artifactTypeFilters.sortField, this.stateService.artifactTypeFilters.sortOrder, this.stateService.artifactTypeFilters.filters, this.stateService.artifactTypeFilters.relationships, this.stateService.artifactTypeFilters.attributes, this.stateService.artifactTypeFilters.simpleTextFilter, this.stateService.artifactTypeFilters.owners);
    }

    customExport() {
        //show the custom export screen        
        this.showCustomExport = !this.showCustomExport;
    }

    saveItem(event) {
        this.isLoading = true; 
        this.showEditor = false;              
        let values: any = {};

        //takes the form and convert any array values to , separated string values
        for (var p in event.item) {            
            if (event.item.hasOwnProperty(p)) {                
                if (Array.isArray(event.item[p])) {                    
                    values[p] = event.item[p].join();
                }                
                else {                                        
                    values[p] = event.item[p];
                }                
            }
        }

        this.artifactService.saveArtifact(values)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);                
                //reload grid for now as the name / id of the field differs in display mode / edit mode
                if(event.item.ID) this.headerActionsService.emitFavoritesChange(); // favorites need to be reloaded if an object was edited
                this.getData();                
                this.isLoading = false;
            });
    }

    selectArtifact(artifact) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('Artifact', artifact.ID, this.artifactType.ID));
    }
        
    private loadArtifactsLazy(event: LazyLoadEvent) {
        //event.first = First row offset
        //event.rows = Number of rows per page
        //event.sortField = Field name to sort with
        //event.sortOrder = Sort order as number, 1 for asc and -1 for dec
        //filters: FilterMetadata object having field as key and filter value, filter matchMode as value        
        this.stateService.artifactTypeFilters.sortOrder = event.sortOrder;
        this.stateService.artifactTypeFilters.sortField = event.sortField == undefined ? "" : event.sortField;
        this.rowsPerPage = event.rows;
        this.stateService.artifactTypeFilters.currentPageNumber = event.first / event.rows;
        this.getData();
    }

    private checkSimpleSearchEnter(event, dt: DataTable) {
        if (event.keyCode == 13) this.doSimpleSearch(dt);
        else {
            if (this.simpleSearchID > 0) {                
                window.clearTimeout(this.simpleSearchID);
                this.simpleSearchID = 0;
            }            
            this.simpleSearchID = window.setTimeout(() => this.doSimpleSearch(dt), this.searchDelayMilliSeconds);            
        }
    }

    private doSimpleSearch(dt: DataTable) {
        if (this.isGridFilterLoading) {
            if (this.simpleSearchID > 0) {
                window.clearTimeout(this.simpleSearchID);
                this.simpleSearchID = 0;
            }      
            this.simpleSearchID = window.setTimeout(() => this.doSimpleSearch(dt), this.searchDelayMilliSeconds); //check back in a few search is ongoing
            return;
        }
        this.isGridFilterLoading = true;
        if (dt) dt.reset();
        //this.getData();  
    }

    protected certificateColor(item) {
        switch (item.Status) {
            case 'Certified':
                return 'artifact-certification-certified';
            case 'Under Review':
                return 'artifact-certification-underreview';
        }
        return 'artifact-certification';
    }

    protected onRightClick(event,rightMenu,artifact, grid) {
        this.isMenuOpen = true;        
        var gridRect = grid.el.nativeElement.getBoundingClientRect();
        var itemRect = event.srcElement.getBoundingClientRect();
        rightMenu.style.top = (event.screenY - gridRect.top) + 'px';    
        rightMenu.style.left = (event.offsetX) + 'px'; //correct
        this.itemUrl = SiteUrlHelpers.getObjectUrl('Artifact', artifact.ID, this.artifactType.ID);        
        return false;
    }

    clickedOutside() {
        if (this.isMenuOpen) {        
            this.isMenuOpen = false;
        }
    }

    private toggleArtifactDetail() {
        this.showArtifactDetails = !this.showArtifactDetails;
    }
}


