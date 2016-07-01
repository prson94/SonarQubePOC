///<reference path="../../es6-shim.d.ts"/>
import { Component} from '@angular/core';
import {DataTable, Column} from 'primeng/primeng';
import { MessagesService, HeaderBreadcrumbService, PageHeader, RelationshipsService  } from '../../services/index';
import {AdminBaseComponent} from './admin-base.component';
import { TileActionsComponent } from '../tiles/tile-actions.component';
import { PredicatesTile } from '../tiles/predicates.tile';
import { FieldDefinitionTile } from '../tiles/field-definition.tile';
import { Relationship } from '../../models/relationship.model';


@Component({
    selector: 'd3s-admin-relationships-component',
    directives: [DataTable, Column, TileActionsComponent, PredicatesTile, FieldDefinitionTile],
    providers: [RelationshipsService],
    template: `<div class="row">
                    <div class="col l6 s12">                    
                        <div class="tile tile-detail">
                            <header>Relationship Types
                                <d3s-tile-actions [hasAdd]="true" [addTitle]="'Add Relationship'" (addClick)="add()"></d3s-tile-actions>                            
                            </header>                                                                               
                            <p-dataTable *ngIf="!showEditor && !showDelete && !isLoading" [value]="relationships" selectionMode="single" [rows]="20" [paginator]="true" [pageLinks]="3" expandableRows="true" [(selection)]="selected"  (onRowDblclick)="selected=$event.data;showEditor=true;" >                                                                                        
                                <p-column field="Source" header="Source Type" [sortable]="true" [filter]="true"></p-column>                                
                                <p-column field="SourceName" header="Source Name" [sortable]="true" [filter]="true"></p-column>
                                <p-column field="Target" header="Target Type" [sortable]="true" [filter]="true"></p-column>                                
                                <p-column field="TargetName" header="Target Name" [sortable]="true" [filter]="true"></p-column>
                            </p-dataTable>   
                        </div>
                    </div>                    
                    <div class="col l6 s12">
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">
                                    <d3s-predicates-tile></d3s-predicates-tile>
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">                                              
                                    <d3s-field-definition-tile [objectType]="'IntersectType'" [objectID]="selected?.ID" ></d3s-field-definition-tile>
                                </div>
                            </div>
                        </div>
                    <div>                    
                </div>  
                `
})

export class AdminRelationshipsComponent extends AdminBaseComponent {
    relationships: Relationship[] = [];

    selected: Relationship;
    showEditor: boolean = false;
    showDelete: boolean = false;

    constructor(private relationshipsService: RelationshipsService, protected messagesService: MessagesService, headerBreadcrumbService: HeaderBreadcrumbService, pageHeader: PageHeader) {
        super(headerBreadcrumbService, pageHeader);
        this.areaDescription = "Create the possibility of establishing relationships between different objects within the system.";
        this.areaName = "Relationship Types";
        this.setCommonItems();
    }

    ngOnInit() {
        this.getRelationships();
    }

    getRelationships() {
        this.isLoading = true;
        this.relationshipsService.getRelations()
            .then(result => {
                this.relationships = result;
                this.isLoading = false;
                if (this.relationships.length > 0) this.selected = this.relationships[0];
            });
    }

    add() { }
}