///<reference path="../../es6-shim.d.ts"/>
import { Component, Input, Output, OnChanges, SimpleChange, EventEmitter} from '@angular/core';
import {DataTable, Column} from 'primeng/primeng';
import { Lookup, LookupItem } from '../../models/lookup.model';
import { GridDefinition, GridColumn } from '../../models/grid-definition.model';
import { MessagesService, GridDefinitionService, UriBasedService} from '../../services/index';
import { TileActionsComponent } from '../tiles/tile-actions.component';
import {DeleteForm} from '../forms/delete.form';
import { DynamicEditorComponent } from './dynamic-editor.component';


@Component({
    selector: 'd3s-dynamic-grid',
    directives: [DataTable, Column, TileActionsComponent, DeleteForm, DynamicEditorComponent],
    providers: [GridDefinitionService, UriBasedService],
    template: ` 
                <header *ngIf="!showEditor && !showDelete">{{title}}
                    <d3s-tile-actions [hasAdd]="showAddButton" [addTitle]="'Add ' + title" (addClick)="add()"></d3s-tile-actions>                            
                </header>           
                <div *ngIf="isLoading" style="width:100%; text-align:center;">
                    <div style="padding:10px;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                </div>           
               <p-dataTable *ngIf="!isLoading && !showDelete && !showEditor" [value]="items" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" expandableRows="true" (onRowDblclick)="selected=$event.data;editItemClick.emit(selectedItem)" [(selection)]="selected" >                                                                       
                    <p-column *ngFor="let column of columns" [field]="column.datafield" [header]="column.text" [filter]="column.filterable" [sortable]="column.sortable"></p-column>
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
                <d3s-dynamic-editor *ngIf="showEditor" [objectID]="objectID" [objectType]="objectType" [title]="itemName + ' Item'" [selection]="selected" [rowID]="rowID" (saveClick)="saveItem($event)" (closeClick)="closeEditor()"></d3s-dynamic-editor>
                <delete-form *ngIf="showDelete"
                    [callback]="theDeleteCallback"
                    [itemId]="selected?.ID"
                    [method]="'callback'"
                    [prompt]="'Are you sure you want to delete the selected item?'"                                         
                    (onCancel)="showDelete=false;"
                ></delete-form>                                    
                `
})

export class DynamicGridComponent implements OnChanges {
    @Input() objectType: string;
    @Input() rowID: string = 'ID';
    @Input() objectID: number;
    @Input() dataUri: string;
    @Input() deleteUri: string;
    @Input() createUri: string;
    @Input() editUri: string;
    @Input() title: string = "Items";
    @Input() itemName: string = "";

    @Input() showEditButton: boolean = true;
    @Input() showDeleteButton: boolean = true;
    @Input() showAddButton: boolean = true;
    
    error: any;
    items: any[] = [];
    columns: GridColumn[] = [];   

    showDelete: boolean = false;
    showEditor: boolean = false;
    isLoading: boolean = false;
    
    selected: any = null;

    theDeleteCallback: Function;
    

    constructor(private gridDefinitionService: GridDefinitionService, private uriBasedService: UriBasedService) {
        this.theDeleteCallback = this.deleteItem.bind(this);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.objectID != null && this.objectType != null) this.load();
    }
    
    load() {
        this.getFieldsDefinition();        
        this.getData();
    }

    deleteItem(id: number) {
        this.uriBasedService.deleteItem(this.deleteUri, id);   
        this.showDelete = false;
        if (this.items.length > 0) this.items.splice(this.findItemIndex(id), 1);
    }

    getFieldsDefinition() {        
        this.gridDefinitionService.getGridDefinition(this.objectID, this.objectType)
            .then(result => {
                this.columns = result.Columns;                
            });
    }    

    getData() {    
        this.isLoading = true;
        this.uriBasedService.getItems(this.dataUri)
            .then(result => {
                this.items = result;                
                this.isLoading = false;
                if (this.items.length > 0) this.selected = this.items[0];
                console.log(this.items);
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
}


