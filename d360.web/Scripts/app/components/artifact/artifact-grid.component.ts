///<reference path="../../es6-shim.d.ts"/>
import { Component, Input, Output, OnChanges, SimpleChange, EventEmitter, OnInit} from '@angular/core';
import { LazyLoadEvent } from 'primeng/primeng';
import { Lookup, LookupItem } from '../../models/lookup.model';
import { GridDefinition, GridColumn, GridField, GridFilterColumn, GridFilterExpression, GridRelationshipFilterExpression, GridAttributeFilterExpression } from '../../models/grid-definition.model';
import { MessagesService, GridDefinitionService, UriBasedService, ArtifactService, PermissionsService} from '../../services/index';
import { ArtifactType } from '../../models/artifact-type.model';
import { SortOrder } from '../../models/enums.model';
import { Router, ActivatedRoute }       from '@angular/router';


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
                <header *ngIf="!showEditor && !showDelete">{{artifactType?.Name}}
                    <d3s-tile-actions [hasAdd]="showAddButton" [hasExport]="true" (addClick)="add()" (exportClick)="export()"></d3s-tile-actions>                            
                </header>           
                <div *ngIf="isLoading" style="width:100%; text-align:center;">
                    <div style="padding:10px;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                </div>    
                <div class="row" *ngIf="!isLoading && !showDelete && !showEditor" >       
                    <div class="col s12">                                                
                        <div class="search-input-container" style="padding-bottom:10px;">
                            <div class="search-input-text-container" style="padding-left:0;">
                                <input type="text" (keyup)="checkSimpleSearchEnter($event);" [(ngModel)]="simpleSearchValue" placeholder="Search..." class="search-input-text" autofocus autocomplete="off" />
                            </div>                            
                            <div class="search-input-button-container">
                                <button type="button" name="action" id="home-search-btn" class="search-input-btn" (click)="doSimpleSearch()">
                                    <i class="fa fa-search"></i>
                                </button>
                            </div>
                        </div>
                    </div>
                    
                    <div *ngIf="showTypeFilter" class="col l10 m9 s12">                                                                         
                        <input type="text" [(ngModel)]="searchValue" placeholder="Search" style="width: 100%;">
                    </div>
                    <div *ngIf="showTypeFilter" class="col l2 m3 s12">                                                                         
                        <button [disabled]="!searchValue" pButton type="button" (click)="searchValue='';" label="Clear" style="width: 100%;"></button>
                    </div>
                    <d3s-artifact-column-filter [artifactType]="artifactType" [fields]="filtercolumns" (filterChanged)="filterGridData($event)"></d3s-artifact-column-filter>
                    <div class="col s12">
                       <p-dataTable [lazy]="true" [totalRecords]="totalRecords"  scrollable="true" scrollWidth="100%" [value]="items" selectionMode="single" [rows]="rowsPerPage" [paginator]="true" [pageLinks]="4" (onRowDblclick)="selectArtifact($event.data)" [(selection)]="selected" (onLazyLoad)="loadArtifactsLazy($event)" [rowsPerPageOptions]="[5,10,20]" [responsive]="true" [stacked]="stacked">                                                                       
                            <p-column *ngFor="let column of columns" [field]="column.datafield" [header]="column.text" [sortable]="column.sortable"  [style]="{'width':'250px'}">
                                <template let-col let-item="rowData" pTemplate type="body">
                                        <div [innerHtml]="item[column.datafield]"></div>
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
                <d3s-dynamic-editor *ngIf="showEditor" [objectID]="artifactType?.ID" [parentID]="artifactType?.ParentID" [objectType]="'Artifact'" [title]="artifactType?.Name + ' Item'" [selection]="selected" [rowID]="rowID" (saveClick)="saveItem($event)" (closeClick)="closeEditor()"></d3s-dynamic-editor>
                <delete-form *ngIf="showDelete"
                            [callback]="theDeleteCallback"
                            [itemId]="selected?.ID"
                            [method]="'callback'"
                            [prompt]="'Are you sure you want to delete the selected item?'"                                         
                            (onCancel)="showDelete=false;"
                ></delete-form>  
                `
})



export class ArtifactGridComponent implements OnChanges {    
    @Input() rowID: string = 'ID';
    @Input() artifactType: ArtifactType;
        
    showEditButton: boolean = true;
    showDeleteButton: boolean = true;
    showAddButton: boolean = true;
    showTypeFilter: boolean = false;

    totalRecords: number;
    rowsPerPage: number = 20;
    currentPageNumber: number = 0;
    sortField: string = "";
    sortOrder: SortOrder = SortOrder.None;
    searchValue: string = "";
    simpleSearchValue: string = "";
    searchDelayMilliSeconds: number = 1000;

    error: any;
    items: any[];
    columns: GridColumn[] = [];
    fields: GridField[] = [];
    filtercolumns: GridFilterColumn[] = [];
    filters: GridFilterExpression[] = [];
    relationships: GridRelationshipFilterExpression;
    attributes: GridAttributeFilterExpression;

    showDelete: boolean = false;
    showEditor: boolean = false;
    isLoading: boolean = false;

    simpleSearchID: number = 0;

    selected: any = null;

    theDeleteCallback: Function;
    
    constructor(private permissionsService: PermissionsService, private router: Router, private gridDefinitionService: GridDefinitionService, private uriBasedService: UriBasedService, private artifactService: ArtifactService) {
        this.theDeleteCallback = this.deleteItem.bind(this);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.artifactType != null) {
            this.load();
        }
    }

    load() {
        this.getFieldsDefinition();              
    }

    filterGridData(filterData) {
        if (filterData.filter)
            this.filters = filterData.filter;
        else {
            this.filters.splice(0, this.filters.length);
        }

        if (filterData.relationships) {
            this.relationships = filterData.relationships;
        }
        else {
            this.relationships = null;
        }

        if (filterData.attributes) {
            this.attributes = filterData.attributes;
        }
        else {
            this.attributes = null;
        }


        this.currentPageNumber = 0;
        this.getData();
    }

    deleteItem(id: number) {
        this.uriBasedService.deleteItem("form/dynamicedit/delete/artifact/", id);
        this.showDelete = false;
        if (this.items.length > 0) {            
            this.items.splice(this.findItemIndex(id), 1);
        }
    }

    getFieldsDefinition() {
        this.gridDefinitionService.getGridDefinition(this.artifactType.ID, 'ArtifactType')
            .then(result => {
                this.columns = result.Columns;
                this.filtercolumns = result.FilterColumns;
            });
    }
    
    getData() {
        this.artifactService.getArtifacts(this.artifactType, this.rowsPerPage, this.currentPageNumber, this.sortField, this.sortOrder, this.filters, this.relationships, this.attributes, this.simpleSearchValue)
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
        this.router.navigateByUrl(`/a/artifact/${this.artifactType.ID}/${artifact.ID}`)
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
        
        this.sortOrder = event.sortOrder;
        this.sortField = event.sortField == undefined ? "" : event.sortField;
        this.rowsPerPage = event.rows;
        this.currentPageNumber = event.first / event.rows;
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
}


