///<reference path="../../es6-shim.d.ts"/>
import { Component} from '@angular/core';
import {DataTable, Column} from 'primeng/primeng';
import { MessagesService, HeaderBreadcrumbService, PageHeader  } from '../../services/index';
import {AdminBaseComponent} from './admin-base.component';
import { TileActionsComponent } from '../tiles/tile-actions.component';
import { FieldDefinition } from '../../models/fields.model';
import {DeleteForm} from '../forms/delete.form';


@Component({
    selector: 'd3s-admin-lookups-component',
    directives: [DataTable, Column, TileActionsComponent],
    providers: [],
    template: `<div class="row">
                    <div class="col l4 s12">                    
                        <div class="tile tile-detail">
                            <header *ngIf="!showEditor">Lookup Types
                                <d3s-tile-actions [hasAdd]="true" [addTitle]="'Add Lookup'" (addClick)="add()"></d3s-tile-actions>                            
                            </header>
                            <p-dataTable *ngIf="!showEditor && !showDelete" [value]="lookups" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" expandableRows="true" [(selection)]="selectedTaxonomy"  (onRowDblclick)="showEditor=true;" >                                                        
                                <p-column field="ID" header="ID" [sortable]="true" [filter]="true"></p-column>                                                            
                                <p-column field="Name" header="Name" [sortable]="true" [filter]="true"></p-column>                            
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
                        </div>
                    </div>                    
                </div>  
                `
})

export class AdminLookupsComponent extends AdminBaseComponent {
    
    constructor(protected messagesService: MessagesService, headerBreadcrumbService: HeaderBreadcrumbService, pageHeader: PageHeader) {
        super(headerBreadcrumbService, pageHeader);        
        this.areaDescription = "Here you will find all general lookups used.";
        this.areaName = "Lookup Types";
        this.setCommonItems();
    }

    ngOnInit() {
        
        
    }
    
}