import { Component, Input, Output, OnChanges, SimpleChange, EventEmitter, OnInit, ViewChild} from '@angular/core';
import { LazyLoadEvent } from 'primeng/primeng';
import { Lookup, LookupItem } from '../../models/lookup.model';
import { GridDefinition, GridColumn, GridField, GridFilterColumn, GridFilterExpression, GridRelationshipFilterExpression, GridAttributeFilterExpression } from '../../models/grid-definition.model';
import { MessagesService, GridDefinitionService, UriBasedService, ArtifactService, PermissionsService, StateService} from '../../services/index';
import { ArtifactType } from '../../models/artifact-type.model';
import { Router, ActivatedRoute }       from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { ArtifactColumnFilterComponent } from './artifact-column-filter.component'
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { StringConstants } from '../../static/string-constants';

@Component({
    selector: 'd3s-artifact-grid',
    providers: [GridDefinitionService, UriBasedService, ArtifactService, PermissionsService],
    styles: [`
           .simple-search{
                width:100%;
                padding:10px;
                border: 1px solid #CCCCCC;
                border-radius: 5px;
                margin: 5px;
            }
        `],
    template: ` 
                <header *ngIf="!showEditor && !showDelete">{{artifactType?.Name}}{{titlePostfix}}
                    <d3s-tile-actions [hasAdd]="showAddButton && hasRootCreatePermissions()" [hasExport]="true" (addClick)="add()" (exportClick)="export()" [hasFilterMode]="true" [filterMode]="stateService.artifactTypeFilters.showSimpleFilter" (filterModeChange)="stateService.artifactTypeFilters.showSimpleFilter=$event;resetFilters();"></d3s-tile-actions>                            
                </header>           
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div class="row" *ngIf="!isLoading && !showDelete && !showEditor" >       
                    <div class="col s12" *ngIf="stateService.artifactTypeFilters.showSimpleFilter">                                                
                        <input type="text" style="width: 100%;" (keyup)="checkSimpleSearchEnter($event);" [(ngModel)]="stateService.artifactTypeFilters.simpleTextFilter" placeholder="Search..." autofocus autocomplete="off" />                            
                    </div>                                        
                    <d3s-artifact-column-filter [hidden]="stateService.artifactTypeFilters.showSimpleFilter" [(attributeFilter)]="stateService.artifactTypeFilters.attributes" [(relationshipFilter)]="stateService.artifactTypeFilters.relationships" [(filters)]="stateService.artifactTypeFilters.filters" [artifactType]="artifactType" [fields]="filtercolumns" (filterChanged)="filterGridData($event)"></d3s-artifact-column-filter>
                    <div class="col s12">
                       <p-dataTable [lazy]="true" [totalRecords]="totalRecords"  scrollable="true" scrollWidth="100%" [value]="items" selectionMode="single" [rows]="rowsPerPage" [paginator]="true" [pageLinks]="4" (onRowDblclick)="selectArtifact($event.data)" [(selection)]="selected" (onLazyLoad)="loadArtifactsLazy($event)" [rowsPerPageOptions]="[5,10,20]" [responsive]="true" [stacked]="stacked">                                                                       
                            <p-column field="Name" header="Name" [sortable]="true"  [style]="{'width':'250px'}">
                                <template let-item="rowData" pTemplate type="body">
                                    <a (click)="selectArtifact(item)">{{item.Name}}</a>
                                </template>
                            </p-column>
                            <p-column *ngFor="let column of columns" [field]="column.datafield" [header]="column.text" [sortable]="column.sortable"  [style]="{'width':'250px'}">                                
                                <template let-item="rowData" pTemplate type="body">
                                    <span [ngSwitch]="columnDataType(column)">
                                        <span *ngSwitchCase="'date'">{{item[column.datafield] | date:'short'}}</span>
                                        <span *ngSwitchCase="'bool'">
                                            <i *ngIf="item[column.datafield]" class="fa fa-check enabled" title="True"></i>
                                            <i *ngIf="!item[column.datafield]" class="fa fa-times disabled" title="False"></i>
                                        </span>
                                        <span *ngSwitchDefault [innerHtml]="item[column.datafield]"></span>                                        
                                    </span>
                                </template>
                            </p-column>
                            <p-column [style]="{width:'40px'}">
                                    <template let-item="rowData" pTemplate type="body">
                                        <div class="RowTools">
                                            <d3s-tooltip [objectType]="'Artifact'" [objectId]="item.ID" [tooltipType]="'certificate'" [icon]="'certificate'" [iconColor]="certificateColor(item)"></d3s-tooltip>                                            
                                        </div>
                                    </template>
                            </p-column>
                            <p-column [style]="{width:'40px'}">
                                    <template let-item="rowData" pTemplate type="body">
                                        <div class="RowTools">
                                            <d3s-tooltip [objectType]="'Artifact'" [objectId]="item.ID" (click)="selectArtifact(item)" [tooltipType]="'Preview'" [icon]="'info'"></d3s-tooltip>                                            
                                        </div>
                                    </template>
                            </p-column>
                            <p-column [style]="{width:'40px'}" *ngIf="showEditButton">
                                    <template let-item="rowData" pTemplate type="body">
                                        <div class="RowTools">
                                            <a style="cursor:pointer;" (click)="selected=item;showEditor=true;"><i class="fa fa-pencil"></i></a>                                        
                                        </div>
                                    </template>
                            </p-column>                            
                            <p-column  [style]="{width:'40px'}" *ngIf="showDeleteButton">
                                    <template let-item="rowData" pTemplate type="body">
                                        <div class="RowTools">                                
                                            <a style="cursor:pointer;" (click)="selected=item;showDelete=true;"><i class="fa fa-trash-o"></i></a>                                    
                                        </div>
                                    </template>
                            </p-column>                            
                        </p-dataTable>                           
                    </div>
                </div>                                  
                <d3s-dynamic-editor *ngIf="showEditor" [objectID]="artifactType?.ID" [objectType]="'Artifact'" [title]="artifactType?.Name + ' Item'" [selection]="selected" [rowID]="rowID" (saveClick)="saveItem($event)" (closeClick)="closeEditor()"></d3s-dynamic-editor>
                <delete-form *ngIf="showDelete"
                            [callback]="theDeleteCallback"
                            [itemId]="selected?.ID"
                            [method]="'callback'"
                            [prompt]="'Are you sure you want to delete the selected item?'"                                         
                            (onCancel)="showDelete=false;"
                ></delete-form>  
                `
})



export class ArtifactGridComponent extends BaseComponent implements OnChanges {    
    @Input() rowID: string = 'ID';
    @Input() artifactType: ArtifactType;
    @Input() titlePostfix: string = ''; // added to end of header title.
    @Input() rowsPerPage: number = 20;
        
    @ViewChild(ArtifactColumnFilterComponent) private filtersComponent: ArtifactColumnFilterComponent;
        
    showEditButton: boolean = true;
    showDeleteButton: boolean = true;
    showAddButton: boolean = true;
    
    totalRecords: number;
        
    searchValue: string = "";
    
    searchDelayMilliSeconds: number = 1000;

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
    
    constructor(private stateService: StateService, private permissionsService: PermissionsService, private router: Router, private gridDefinitionService: GridDefinitionService, private uriBasedService: UriBasedService, private artifactService: ArtifactService) {
        super();
        this.theDeleteCallback = this.deleteItem.bind(this);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.artifactType != null) {
            this.load();
        }

        //clear out the filters if the artifacttype is different
        this.stateService.resetArtifactTypeFilterIfRequired(this.artifactType.ID);        
    }

    load() {
        this.loadPermissions(this.permissionsService, StringConstants.ObjectArtifactType, this.artifactType.ID);
        this.getFieldsDefinition();              
    }

    public filterGridData(filterData) {
        this.stateService.artifactTypeFilters.currentPageNumber = 0;
        this.getData();
    }

    resetFilters() {        
        this.stateService.artifactTypeFilters.simpleTextFilter = '';
        this.filtersComponent.resetFilters();
    }

    deleteItem(id: number) {
        this.uriBasedService.deleteItem("form/dynamicedit/delete/artifact/", id);
        this.showDelete = false;
        if (this.items.length > 0) {            
            this.items.splice(this.findItemIndex(id), 1);
        }
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
        this.artifactService.getArtifacts(this.artifactType.ID, this.rowsPerPage, this.stateService.artifactTypeFilters.currentPageNumber, this.stateService.artifactTypeFilters.sortField, this.stateService.artifactTypeFilters.sortOrder, this.stateService.artifactTypeFilters.filters, this.stateService.artifactTypeFilters.relationships, this.stateService.artifactTypeFilters.attributes, this.stateService.artifactTypeFilters.simpleTextFilter)
            .then(result => {
                this.items = result.results;
                this.totalRecords = result.total;                
                if (this.items.length > 0) this.selected = this.items[0]; 
                this.simpleSearchID = 0;               
            });
    }

    private certificateColor(item) {        
        switch (item.Status) {
            case 'Certified':
                return '#3f9d40';                
            case 'Under Review':
                return '#e2792a';                             
        }
        return '#ebebeb';
    }

    private findItemIndex(id: number) {
        var index: number = -1;
        for (var item of this.items) {
            index++;
            if (item.ID == id) return index;
        }
    }

    closeEditor() {
        this.showEditor = false;
    }

    add() {
        this.selected = null;
        this.showEditor = true;
    }

    export() {
        this.artifactService.getArtifactsXls(this.artifactType);
    }

    saveItem(event) {
        this.isLoading = true;        
        this.uriBasedService.saveItem("form/dynamicedit/create/artifact", "form/dynamicedit/edit/artifact", event.item)
            .then(result => {
                //reload grid for now as the name / id of the field differs in display mode / edit mode
                this.getData();
                this.showEditor = false;
                this.isLoading = false;
            });
    }

    selectArtifact(artifact) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('Artifact', artifact.ID, this.artifactType.ID));
    }

    simpleSearchChanged(event) {
        console.log(event);
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

    private checkSimpleSearchEnter(event) {
        if (event.keyCode == 13) this.doSimpleSearch();
        else {
            if (this.simpleSearchID > 0) {
                window.clearTimeout(this.simpleSearchID);
                this.simpleSearchID = 0;
            }

            this.simpleSearchID = window.setTimeout(() => this.doSimpleSearch(), this.searchDelayMilliSeconds);
            
        }
    }

    private doSimpleSearch() {        
        this.getData();
    }

    private columnDataType(column: GridColumn): string {
        var fields = this.fields.filter(x => x.name == column.datafield);

        if (fields.length > 0)
            return fields[0].type;
        return 'string';
    }
}


