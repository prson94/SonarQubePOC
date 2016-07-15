///<reference path="../../es6-shim.d.ts"/>
import { Component, Input, Output, OnChanges, SimpleChange, EventEmitter} from '@angular/core';
import {DataTable, Column, LazyLoadEvent, Button} from 'primeng/primeng';
import { Lookup, LookupItem } from '../../models/lookup.model';
import { GridDefinition, GridColumn } from '../../models/grid-definition.model';
import { MessagesService, GridDefinitionService, UriBasedService, ArtifactService} from '../../services/index';
import { TileActionsComponent } from '../tiles/tile-actions.component';
import {DeleteForm} from '../forms/delete.form';
import { DynamicEditorComponent } from '../shared/dynamic-editor.component';
import { ArtifactType } from '../../models/artifact-type.model';
import { SortOrder } from '../../models/enums.model';
import { Router, ActivatedRoute }       from '@angular/router';


@Component({
    selector: 'd3s-artifact-grid',
    directives: [DataTable, Column, TileActionsComponent, DeleteForm, DynamicEditorComponent, Button],
    providers: [GridDefinitionService, UriBasedService, ArtifactService],
    template: ` 
                <header *ngIf="!showEditor && !showDelete">{{artifactType?.Name}}
                    <d3s-tile-actions [hasAdd]="showAddButton" [addTitle]="'Add ' + artifactType?.Name" (addClick)="add()"></d3s-tile-actions>                            
                </header>           
                <div *ngIf="isLoading" style="width:100%; text-align:center;">
                    <div style="padding:10px;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                </div>    
                <div class="row" *ngIf="!isLoading && !showDelete && !showEditor" >       
                    <div *ngIf="showTypeFilter" class="col l10 m9 s12">                                                                         
                        <input type="text" [(ngModel)]="searchValue" placeholder="Search" style="width: 100%;">
                    </div>
                    <div *ngIf="showTypeFilter" class="col l2 m3 s12">                                                                         
                        <button [disabled]="!searchValue" pButton type="button" (click)="searchValue='';" label="Clear" style="width: 100%;"></button>
                    </div>
                    <div class="col s12">
                       <p-dataTable [lazy]="true" [totalRecords]="totalRecords" [value]="items" selectionMode="single" [rows]="rowsPerPage" [paginator]="true" [pageLinks]="4" expandableRows="true" (onRowDblclick)="selectArtifact($event.data)" [(selection)]="selected" (onLazyLoad)="loadArtifactsLazy($event)" [rowsPerPageOptions]="[5,10,20]">                                                                       
                            <p-column *ngFor="let column of columns" [field]="column.datafield" [header]="column.text" [filter]="false" [sortable]="true"></p-column>
                            <p-column [style]="{width:'40px'}" *ngIf="showEditButton">
                                    <template let-item="rowData">
                                        <div class="RowTools">
                                            <a style="cursor:pointer;" (click)="selected=item;showEditor=true;"><i class="fa fa-pencil"></i></a>                                        
                                        </div>
                                    </template>
                            </p-column>                            
                            <p-column  [style]="{width:'40px'}" *ngIf="showDeleteButton">
                                    <template let-item="rowData">
                                        <div class="RowTools">                                
                                            <a style="cursor:pointer;" (click)="selected=item;showDelete=true;"><i class="fa fa-trash-o"></i></a>                                    
                                        </div>
                                    </template>
                            </p-column>                            
                        </p-dataTable>                           
                    </div>
                </div>                                  
                <d3s-dynamic-editor *ngIf="showEditor" [objectID]="objectID" [objectType]="objectType" [title]="artifactType?.Name + ' Item'" [selection]="selected" [rowID]="rowID" (saveClick)="saveItem($event)" (closeClick)="closeEditor()"></d3s-dynamic-editor>
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
    
    @Input() deleteUri: string;
    @Input() createUri: string;    
    
    objectType: string = 'ArtifactType';
    editUri: string = 'form/dynamicedit/edit/artifact/';

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

    error: any;
    items: any[];
    columns: GridColumn[] = [];

    showDelete: boolean = false;
    showEditor: boolean = false;
    isLoading: boolean = false;

    selected: any = null;

    theDeleteCallback: Function;
    
    constructor(private router: Router, private gridDefinitionService: GridDefinitionService, private uriBasedService: UriBasedService, private artifactService: ArtifactService) {
        this.theDeleteCallback = this.deleteItem.bind(this);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.artifactType != null) this.load();
    }

    load() {
        this.getFieldsDefinition();
      //  this.getData();
    }

    deleteItem(id: number) {
        this.uriBasedService.deleteItem(this.deleteUri, id);
        this.showDelete = false;
        if (this.items.length > 0) this.items.splice(this.findItemIndex(id), 1);
    }

    getFieldsDefinition() {
        this.gridDefinitionService.getGridDefinition(this.artifactType.ID, this.objectType)
            .then(result => {
                this.columns = result.Columns;
            });
    }
    
    getData() {        
        this.artifactService.getArtifacts(this.artifactType, this.rowsPerPage, this.currentPageNumber, this.sortField, this.sortOrder)
            .then(result => {
                this.items = result.results;
                this.totalRecords = result.total;                
                if (this.items.length > 0) this.selected = this.items[0];                
            });
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

    saveItem(event) {
        this.isLoading = true;
        this.uriBasedService.saveItem(this.createUri, this.editUri, event.item)
            .then(result => {
                //reload grid for now as the name / id of the field differs in display mode / edit mode
                this.getData();
                this.showEditor = false;
            });
    }

    selectArtifact(artifact) {
        this.router.navigateByUrl(`/a/artifact/${this.artifactType.ID}/${artifact.ID}`)
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
}


