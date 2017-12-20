import { Component, Input, Output, OnChanges, SimpleChange, EventEmitter} from '@angular/core';
import { Column } from 'primeng/primeng';
import { Lookup, LookupItem } from '../../../models/lookup.model';
import { GridDefinition, GridColumn, GridField } from '../../../models/grid-definition.model';
import { MessagesService } from '../../../services/messages.service';
import { GridDefinitionService } from '../../../services/grid-definition.service';
import { UriBasedService } from '../../../services/uri-based.service';
import { BaseComponent } from '../../shared/base.component';

@Component({
    selector: 'd3s-dynamic-grid',
    providers: [GridDefinitionService, UriBasedService],
    template: ` 
                <header *ngIf="!showEditor && !showDelete">{{title}}
                    <d3s-tile-actions [hasAdd]="showAddButton" (addClick)="add()" hasFilterMode="true" [(filterMode)]="showSimpleFilter" [hasExport]="showExportButton" (exportClick)="exportClick.emit()"></d3s-tile-actions>
                </header>           
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <span *ngIf="!isLoading && !showDelete && !showEditor">
                    <input [hidden]="!showSimpleFilter" #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">                                              
                    <p-dataTable #dt [globalFilter]="gb" [sortField]="sortField" [value]="items" selectionMode="single" [rows]="defaultInitialItemsPerPage" paginator="true" pageLinks="3" (onRowDblclick)="selected=$event.data;editItemClick.emit(selected)" [(selection)]="selected" [rowsPerPageOptions]="defaultPagingOptions">                                                                       
                        <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>
                        <p-column *ngFor="let column of columns" [field]="column.datafield" [header]="column.text" [sortable]="column.sortable ? 'custom' : false" [filter]="!showSimpleFilter"  (sortFunction)="customSort($event, column)">
                            <ng-template let-item="rowData" pTemplate type="body">
                                <d3s-dynamic-field-value [column]="column" [fields]="fields" [item]="item"></d3s-dynamic-field-value>                                                                 
                            </ng-template>
                        </p-column>
                        <p-column [style]="{width:'40px'}" *ngIf="showEditButton">
                                <ng-template let-item="rowData" pTemplate type="body">
                                    <div class="RowTools">
                                        <a style="cursor:pointer;" (click)="selected=item;showEditor=true;"><i class="fa fa-pencil"></i></a>                                        
                                    </div>
                                </ng-template>
                        </p-column>                            
                        <p-column  [style]="{width:'40px'}" *ngIf="showDeleteButton">
                                <ng-template let-item="rowData" pTemplate type="body">
                                    <div class="RowTools">                                
                                        <a style="cursor:pointer;" (click)="selected=item;showDelete=true;"><i class="fa fa-trash-o"></i></a>                                    
                                    </div>
                                </ng-template>
                        </p-column>                            
                    </p-dataTable>   
                </span>
                <d3s-dynamic-editor *ngIf="showEditor" [objectID]="objectID" [objectType]="objectType" [title]="itemName + ' Item'" [selection]="selected" [rowID]="rowID" (saveClick)="saveItem($event)" (closeClick)="closeEditor()"></d3s-dynamic-editor>
                <d3s-delete-form *ngIf="showDelete"
                    [callback]="theDeleteCallback"
                    [itemId]="selected?.ID"
                    [method]="'callback'"
                    [prompt]="'Are you sure you want to delete the selected item?'"                                         
                    (onCancel)="showDelete=false;"
                ></d3s-delete-form>                                    
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
    @Input() sortField: string;

    @Input() showEditButton: boolean = true;
    @Input() showDeleteButton: boolean = true;
    @Input() showAddButton: boolean = true;
    @Input() showExportButton: boolean = false;

    @Output() editItemClick = new EventEmitter();
    @Output() exportClick = new EventEmitter();
    
    error: any;
    items: any[] = [];
    columns: GridColumn[] = [];   
    fields: GridField[] = [];

    showDelete: boolean = false;
    showEditor: boolean = false;
    
    selected: any = null;

    theDeleteCallback: Function;
    

    constructor(private gridDefinitionService: GridDefinitionService, private uriBasedService: UriBasedService, private messagesService: MessagesService) {
        super();
        this.theDeleteCallback = this.deleteItem.bind(this);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.objectID != null && this.objectType != null) this.load();
    }
    
    public load() {
        this.getFieldsDefinition();        
        this.getData();
    }

    deleteItem(id: number) {
        this.uriBasedService.deleteItemWithResult(this.deleteUri, id).
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

    doExport() {

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
                this.showMessageForResult(this.messagesService, result);                                     
                //reload grid for now as the name / id of the field differs in display mode / edit mode
                this.showEditor = false;                
                this.getData();                
            });
    }    

    customSort(e: any, col: any) {
        let field = e.field;
        let direction = e.order;
        
        var fld = this.fields.filter(x => x.name == field);
        var type = (fld != null && fld.length > 0) ? fld[0].type : "";
        
        this.items = this.items.slice().sort((a, b) => {
            let fa = a[field];
            let fb = b[field];

            switch (type) {
                case 'number':
                    let na: number = +fa;
                    let nb: number = +fb;

                    if (na == null || isNaN(na))
                        na = -Infinity;
                    if (nb == null || isNaN(nb))
                        nb = -Infinity;

                    return ((na > nb) ? 1 : (na < nb) ? -1 : 0) * direction;
                case 'date':
                case 'datetime':
                    let da: number = Date.parse(fa);
                    let db: number = Date.parse(fb);

                    if (da == null || isNaN(da))
                        da = new Date(null).getTime();
                    if (db == null || isNaN(db))
                        db = new Date(null).getTime();

                    return ((da > db) ? 1 : (da < db) ? -1 : 0) * direction;
                default:
                    return ((fa > fb) ? 1 : (fa < fb) ? -1 : 0) * direction;
            }
        });

    }
}