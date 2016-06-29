///<reference path="../../es6-shim.d.ts"/>
import { Component, Input, OnChanges, SimpleChange} from '@angular/core';
import {DataTable, Column} from 'primeng/primeng';
import { Lookup, LookupItem } from '../../models/lookup.model';
import { GridDefinition, GridColumn } from '../../models/grid-definition.model';
import { MessagesService, GridDefinitionService} from '../../services/index';
import { TileActionsComponent } from '../tiles/tile-actions.component';


@Component({
    selector: 'd3s-dynamic-grid',
    directives: [DataTable, Column, TileActionsComponent],
    providers: [GridDefinitionService],
    template: `               
               <p-dataTable *ngIf="!isLoading && !showDelete && !showEditor" [value]="items" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" expandableRows="true" (onRowDblclick)="showEditor=true" [(selection)]="selectedItem" >                                                                       
                    <p-column *ngFor="let column of columns" [field]="column.datafield" [header]="column.text" [filter]="true" [sortable]="true"></p-column>
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
                `
})

export class LookupItemsTile implements OnChanges {
    @Input() objectType: string;
    @Input() objectID: number;
    error: any;
    items: any[] = [];
    columns: GridColumn[] = [];    
    isLoading: boolean = false;
    selectedItem: any = null;
    

    constructor(private gridDefinitionService: GridDefinitionService) {
        
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        //if (this.lookup != null) this.load();
    }

    load() {
        this.getFieldsDefinition();        
    }

    getFieldsDefinition() {
        this.isLoading = true;
        this.gridDefinitionService.getGridDefinition(this.objectID, this.objectType)
            .then(result => {
                this.columns = result.Columns;
                this.isLoading = false;
            });
    }    
    
}


