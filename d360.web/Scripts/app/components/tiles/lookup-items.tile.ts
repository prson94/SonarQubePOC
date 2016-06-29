///<reference path="../../es6-shim.d.ts"/>
import { Component, Input, OnChanges, SimpleChange} from '@angular/core';
import {DataTable, Column} from 'primeng/primeng';
import { Lookup, LookupItem } from '../../models/lookup.model';
import { GridDefinition, GridColumn } from '../../models/grid-definition.model';
import { MessagesService, LookupService, GridDefinitionService} from '../../services/index';
import { TileActionsComponent } from '../tiles/tile-actions.component';
import { AdminTaxonomyLevelEditorComponent } from '../admin/admin-taxonomy-level-editor.component';
import {DeleteForm} from '../forms/delete.form';


@Component({
    selector: 'd3s-lookup-items-tile',
    directives: [DataTable, Column, TileActionsComponent, DeleteForm, AdminTaxonomyLevelEditorComponent],
    providers: [LookupService, GridDefinitionService],
    template: `
               <header *ngIf="!showEditor && !showDelete">Items
                <d3s-tile-actions [hasAdd]="true" [addTitle]="'Add lookup item'" (addClick)="add()"></d3s-tile-actions>                            
               </header>
                <div *ngIf="isLoading" style="width:100%; text-align:center;">
                    <div style="padding:10px;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                </div>
               <p-dataTable *ngIf="!isLoading && !showDelete && !showEditor" [value]="items" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" expandableRows="true" (onRowDblclick)="showEditor=true" [(selection)]="selectedItem" >                                                                       
                    <p-column *ngFor="let column of columns" [field]="column.datafield" [header]="column.text" [sortable]="true" [filter]="true"></p-column>
                    <p-column [style]="{width:'40px'}">
                            <template let-template="rowData">
                                <div class="RowTools">
                                    <a style="cursor:pointer;" (click)="showEditor=true"><i class="fa fa-pencil"></i></a>                                        
                                </div>
                            </template>
                    </p-column>                            
                    <p-column  [style]="{width:'40px'}">
                            <template let-template="rowData">
                                <div class="RowTools">                                
                                    <a style="cursor:pointer;" (click)="showDelete=true"><i class="fa fa-trash-o"></i></a>                                    
                                </div>
                            </template>
                    </p-column>                            
                </p-dataTable>      
                <delete-form *ngIf="showDelete"
                    [callback]="theDeleteCallback"
                    [itemId]="selectedLevel?.Level"
                    [method]="'callback'"
                    [prompt]="'Are you sure you want to delete the level [' + [selectedLevel?.Name] + ']?'"                                         
                    (onCancel)="showDelete=false;"
                ></delete-form> 
                <d3s-admin-model-level-editor *ngIf="showEditor" [taxonomyLevel]="selectedLevel" [taxonomy]="taxonomy" (closeClick)="closeEditor()" (saveClick)="saveLevel($event)"></d3s-admin-model-level-editor>                                           
                `
})

export class LookupItemsTile implements OnChanges {
    @Input() lookup: Lookup = null;
    error: any;
    items: LookupItem[] = [];
    columns: GridColumn[] = [];
    showEditor: boolean = false;
    showDelete: boolean = false;
    isLoading: boolean = false;
    selectedItem: LookupItem = null;
    theDeleteCallback: Function;

    constructor(private lookupService: LookupService, private gridDefinitionService: GridDefinitionService) {
        this.theDeleteCallback = this.deleteItem.bind(this);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.lookup != null) this.load();
    }

    load() {
        this.getFieldsDefinition();
        this.getLookupItems();
    }

    getFieldsDefinition() {        
        this.gridDefinitionService.getGridDefinition(this.lookup.ID, "LookupType")
            .then(result => {
                this.columns = result.Columns;                
            });
    }

    getLookupItems() {
        this.isLoading = true;
        this.lookupService.getLookupItems(this.lookup)
            .then(result => {
                this.items = result;                
                this.isLoading = false;
            });
    }

    deleteItem(itemId: number) {

    }    
}


