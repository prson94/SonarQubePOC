
import { Component, Input, Output, OnChanges, SimpleChange, EventEmitter} from '@angular/core';
import { Column } from 'primeng/primeng';
import { Lookup, LookupItem } from '../../models/lookup.model';
import { GridDefinition, GridColumn, GridField } from '../../models/grid-definition.model';
import { MessagesService, GridDefinitionService, UriBasedService } from '../../services/index';
import { BaseComponent } from '../shared/base.component';

@Component({
    selector: 'd3s-dynamic-grid',
    providers: [GridDefinitionService, UriBasedService],
    template: ` 
                <header *ngIf="!showEditor && !showDelete">{{title}}
                    <d3s-tile-actions [hasAdd]="showAddButton" (addClick)="add()" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
                </header>           
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <span *ngIf="!isLoading && !showDelete && !showEditor">
                    <input [hidden]="!showSimpleFilter" #gb type="text" pInputText size="100" placeholder="Search..." style="margin-bottom:10px;width:100%;">                                              
                    <p-dataTable [globalFilter]="gb" [value]="items" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" (onRowDblclick)="selected=$event.data;editItemClick.emit(selected)" [(selection)]="selected" >                                                                       
                        <p-column *ngFor="let column of columns" [field]="column.datafield" [header]="column.text" [sortable]="column.sortable" [filter]="!showSimpleFilter">
                            <template let-item="rowData" pTemplate type="body">
                                <span [ngSwitch]="columnDataType(column)">
                                    <span *ngSwitchCase="'date'">{{item[column.datafield] | date:'short'}}</span>
                                    <span *ngSwitchCase="'bool'">
                                        <i *ngIf="item[column.datafield]" class="fa fa-check enabled" title="True"></i>
                                        <i *ngIf="!item[column.datafield]" class="fa fa-times disabled" title="False"></i>
                                    </span>
                                    <span *ngSwitchDefault>{{item[column.datafield]}}</span>
                                </span>
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
                </span>
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

export class DynamicGridComponent extends BaseComponent implements OnChanges {
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

    @Output() editItemClick = new EventEmitter();
    
    error: any;
    items: any[] = [];
    columns: GridColumn[] = [];   
    fields: GridField[] = [];

    showDelete: boolean = false;
    showEditor: boolean = false;
    
    selected: any = null;

    theDeleteCallback: Function;
    

    constructor(private gridDefinitionService: GridDefinitionService, private uriBasedService: UriBasedService) {
        super();
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
                this.fields = result.Fields;              
            });
    }    

    getData() {    
        this.isLoading = true;
        this.uriBasedService.getItems(this.dataUri)
            .then(result => {
                this.items = result;                
                this.isLoading = false;
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
                this.showEditor = false;                
                this.getData();                
            });
    }

    columnDataType(column: GridColumn) : string {
        var fields = this.fields.filter(x => x.name == column.datafield);

        if (fields.length > 0)
            return fields[0].type;
        return 'string';
    }
}


