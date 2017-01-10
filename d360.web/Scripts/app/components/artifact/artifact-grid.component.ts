import { Component, Input, Output, OnChanges, SimpleChange, EventEmitter, OnInit, ViewChild} from '@angular/core';
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
import { ArtifactColumnFilterComponent } from './artifact-column-filter.component'
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { StringConstants } from '../../static/string-constants';

@Component({
    selector: 'd3s-artifact-grid',
    providers: [GridDefinitionService, ArtifactService, PermissionsService],
    template: ` 
                <header *ngIf="!showEditor && !showDelete">{{artifactType?.Name}}{{titlePostfix}}
                    <d3s-tile-actions [hasAdd]="showAddButton && hasRootCreatePermissions() && !hasSuggest" [hasSuggest]="hasSuggest" (suggestClick)="add()" [hasExport]="true" (addClick)="add()" (exportClick)="export(false)" [hasFilterMode]="true" [filterMode]="stateService.artifactTypeFilters.showSimpleFilter" (filterModeChange)="stateService.artifactTypeFilters.showSimpleFilter=$event;resetFilters();"></d3s-tile-actions>                            
                </header>           
                <d3s-loading [isLoading]="isLoading"></d3s-loading>                
                <div class="row" *ngIf="!isLoading && !showDelete && !showEditor" >       
                    <div class="col s12" *ngIf="stateService.artifactTypeFilters.showSimpleFilter">                                                
                        <input type="text" pInputText style="width: 100%;" maxlength="200" (keyup)="checkSimpleSearchEnter($event,dt);" [(ngModel)]="stateService.artifactTypeFilters.simpleTextFilter" placeholder="Search..." autofocus autocomplete="off" />                            
                    </div>                                        
                    <d3s-artifact-column-filter [hidden]="stateService.artifactTypeFilters.showSimpleFilter" [(attributeFilter)]="stateService.artifactTypeFilters.attributes" [(ownerFilter)]="stateService.artifactTypeFilters.owners" [(relationshipFilter)]="stateService.artifactTypeFilters.relationships" [(filters)]="stateService.artifactTypeFilters.filters" [artifactType]="artifactType" [fields]="filtercolumns" (filterChanged)="filterGridData($event)"></d3s-artifact-column-filter>
                    <d3s-loading [isLoading]="isGridFilterLoading"></d3s-loading>
                    <div class="col s12">
                       <p-dataTable #dt lazy="true" [totalRecords]="totalRecords"  scrollable="true" scrollWidth="100%" [value]="items" selectionMode="single" [rows]="rowsPerPage" paginator="true" pageLinks="3" (onRowDblclick)="selectArtifact($event.data)" [(selection)]="selected" (onLazyLoad)="loadArtifactsLazy($event)" [rowsPerPageOptions]="defaultPagingOptions">
                            <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                            <p-column field="Name" header="Name" sortable="true">
                                <template let-item="rowData" pTemplate type="body">
                                    <a (click)="selectArtifact(item)">{{item.Name}}</a>
                                </template>
                            </p-column>
                            <p-column *ngFor="let column of columns" [field]="column.datafield" [header]="column.text" [sortable]="column.sortable">                                                                
                                <template let-item="rowData" pTemplate type="body">
                                    <d3s-dynamic-field-value [column]="column" [fields]="fields" [item]="item"></d3s-dynamic-field-value>                                 
                                </template>
                            </p-column>
                            <p-column [style]="{width:'30px'}">
                                    <template let-item="rowData" pTemplate type="body">
                                        <div class="RowTools" style="color:red;">
                                            <d3s-tooltip objectType="Artifact" [objectId]="item.ID" tooltipType="certificate" icon="certificate" [class]="certificateColor(item)"></d3s-tooltip>                                            
                                        </div>
                                    </template>
                            </p-column>
                            <p-column [style]="{width:'30px'}">
                                    <template let-item="rowData" pTemplate type="body">
                                        <div class="RowTools">
                                            <d3s-tooltip objectType="Artifact" [objectId]="item.ID" (click)="selectArtifact(item)" tooltipType="Preview" icon="info"></d3s-tooltip>                                            
                                        </div>
                                    </template>
                            </p-column>
                            <p-column [style]="{width:'30px'}" *ngIf="showEditButton">
                                    <template let-item="rowData" pTemplate type="body">
                                        <div class="RowTools">
                                            <a style="cursor:pointer;" (click)="selected=item;showEditor=true;"><i class="fa fa-pencil"></i></a>                                        
                                        </div>
                                    </template>
                            </p-column>                            
                            <p-column  [style]="{width:'35px'}" *ngIf="showDeleteButton">
                                    <template let-item="rowData" pTemplate type="body">
                                        <div class="RowTools">                                
                                            <a style="cursor:pointer;" (click)="selected=item;showDelete=true;"><i class="fa fa-trash-o"></i></a>                                    
                                        </div>
                                    </template>
                            </p-column>                            
                        </p-dataTable>                           
                    </div>
                </div>                                  
                <d3s-dynamic-editor *ngIf="showEditor" [newActionName]="newActionName" [objectID]="artifactType?.ID" objectType="Artifact" [title]="artifactType?.Name + ' Item'" [selection]="selected" [rowID]="rowID" (saveClick)="saveItem($event)" (closeClick)="closeEditor()"></d3s-dynamic-editor>
                <d3s-delete-form *ngIf="showDelete"
                            [callback]="theDeleteCallback"
                            [itemId]="selected?.ID"
                            method="callback"
                            [prompt]="'Are you sure you want to delete ['+ selected?.Name + ']?'"                                         
                            (onCancel)="showDelete=false;"
                ></d3s-delete-form>  
                `
})



export class ArtifactGridComponent extends BaseComponent implements OnChanges {    
    @Input() rowID: string = 'ID';
    @Input() artifactType: ArtifactType;
    @Input() titlePostfix: string = ''; // added to end of header title.
    @Input() rowsPerPage: number = 25;
    @Input() hasSuggest: boolean = false;
        
    @ViewChild(ArtifactColumnFilterComponent) private filtersComponent: ArtifactColumnFilterComponent;
        
    showEditButton: boolean = true;
    showDeleteButton: boolean = true;
    showAddButton: boolean = true;
    isGridFilterLoading: boolean = false;
    
    totalRecords: number;
        
    searchValue: string = "";
    
    searchDelayMilliSeconds: number = 500;

    error: any;
    items: any[];
    columns: GridColumn[] = [];    
    fields: GridField[] = [];
    filtercolumns: GridFilterColumn[] = [];
    
    showDelete: boolean = false;
    showEditor: boolean = false;
    isLoading: boolean = false;

    simpleSearchID: number = 0;

    selected: any = null;

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

    get newActionName(){
        return this.hasSuggest ? "Suggest New" : "New";
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes['artifactType'] && this.artifactType != null) {            
            this.load();
        }

        //clear out the filters if the artifacttype is different
        this.stateService.resetArtifactTypeFilterIfRequired(this.artifactType.ID);        
    }

    load() {
        console.log('load artifact type');
        this.loadPermissions(this.permissionsService, StringConstants.ObjectArtifactType, this.artifactType.ID);
        this.getFieldsDefinition();              
    }

    public filterGridData(filterData) {
        this.isGridFilterLoading = true;
        this.stateService.artifactTypeFilters.currentPageNumber = 0;
        this.getData();
    }

    resetFilters() {        
        this.stateService.artifactTypeFilters.simpleTextFilter = '';
        this.filtersComponent.resetFilters();
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
            });
    }
    
    getData() {        
        this.artifactService.getArtifacts(this.artifactType.ID, this.rowsPerPage, this.stateService.artifactTypeFilters.currentPageNumber, this.stateService.artifactTypeFilters.sortField, this.stateService.artifactTypeFilters.sortOrder, this.stateService.artifactTypeFilters.filters, this.stateService.artifactTypeFilters.relationships, this.stateService.artifactTypeFilters.attributes, this.stateService.artifactTypeFilters.simpleTextFilter, this.stateService.artifactTypeFilters.owners)
            .then(result => {
                this.items = result.results;
                this.totalRecords = result.total;                
                if (this.items.length > 0) this.selected = this.items[0]; 
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

    saveItem(event) {
        this.isLoading = true; 
        this.showEditor = false;              
        this.artifactService.saveArtifact(event.item, this.hasSuggest)
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
        this.isGridFilterLoading = true;
        if (dt) dt.reset();      
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
}


