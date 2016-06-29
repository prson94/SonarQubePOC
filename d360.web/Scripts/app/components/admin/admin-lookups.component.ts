///<reference path="../../es6-shim.d.ts"/>
import { Component} from '@angular/core';
import {DataTable, Column} from 'primeng/primeng';
import { MessagesService, HeaderBreadcrumbService, PageHeader, LookupService  } from '../../services/index';
import {AdminBaseComponent} from './admin-base.component';
import { TileActionsComponent } from '../tiles/tile-actions.component';
import {DeleteForm} from '../forms/delete.form';
import {Lookup} from '../../models/lookup.model';
import { FieldDefinitionTile } from '../tiles/field-definition.tile';
import { LookupItemsTile } from '../tiles/lookup-items.tile';


@Component({
    selector: 'd3s-admin-lookups-component',
    directives: [DataTable, Column, TileActionsComponent, FieldDefinitionTile, DeleteForm, LookupItemsTile],
    providers: [LookupService],
    template: `<div class="row">
                    <div class="col l4 s12">                    
                        <div class="tile tile-detail">
                            <header *ngIf="!showEditor && !showDelete">Lookup Types
                                <d3s-tile-actions [hasAdd]="true" [addTitle]="'Add Lookup'" (addClick)="add()"></d3s-tile-actions>                            
                            </header>
                            <p-dataTable *ngIf="!showEditor && !showDelete" [value]="lookups" selectionMode="single" [rows]="20" [paginator]="true" [pageLinks]="3" expandableRows="true" [(selection)]="selectedLookup"  (onRowDblclick)="showEditor=true;" >                                                        
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
                            <delete-form *ngIf="showDelete"
                                [callback]="theDeleteCallback"
                                [itemId]="selectedLookup?.ID"
                                [method]="'callback'"
                                [prompt]="'Are you sure you want to delete the lookup [' + [selectedLookup?.Name] + ']?'"                                         
                                (onCancel)="showDelete=false;"
                            ></delete-form>                         
                        </div>
                    </div>                    
                    <div class="col l8 s12">
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">                                              
                                    <d3s-field-definition-tile [objectType]="'LookupType'" [objectID]="selectedLookup?.ID" ></d3s-field-definition-tile>
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">                                              
                                    <d3s-lookup-items-tile [lookup]="selectedLookup" ></d3s-lookup-items-tile>
                                </div>
                            </div>
                        </div>
                    <div>
                </div>  
                `
})

export class AdminLookupsComponent extends AdminBaseComponent {
    lookups: Lookup[] = [];
    selectedLookup: Lookup;
    showEditor: boolean = false;
    showDelete: boolean = false;
    theDeleteCallback: Function;

    constructor(private lookupService: LookupService, protected messagesService: MessagesService, headerBreadcrumbService: HeaderBreadcrumbService, pageHeader: PageHeader) {
        super(headerBreadcrumbService, pageHeader);        
        this.areaDescription = "Here you will find all general lookups used.";
        this.areaName = "Lookup Types";
        this.setCommonItems();
    }

    ngOnInit() {
        this.theDeleteCallback = this.deleteLookup.bind(this);
        this.getLookups();
    }

    getLookups() {
        this.isLoading = true;
        this.lookupService.getLookups()
            .then(result => {
                this.lookups = result;
                this.isLoading = false;
                if (this.lookups.length > 0) this.selectedLookup = this.lookups[0];
            });            
    }

    deleteLookup(id: number) {
        this.lookupService.deleteLookup(id);
        this.showDelete = false;
    }
    
}