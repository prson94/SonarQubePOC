///<reference path="../../es6-shim.d.ts"/>
import { Component} from '@angular/core';
import {DataTable, Column, Button} from 'primeng/primeng';
import { MessagesService, HeaderBreadcrumbService, PageHeader, RelationshipsService  } from '../../services/index';
import {AdminBaseComponent} from './admin-base.component';
import { TileActionsComponent } from '../tiles/tile-actions.component';
import { PredicatesTile } from '../tiles/predicates.tile';
import { FieldDefinitionTile } from '../tiles/field-definition.tile';
import { Relationship } from '../../models/relationship.model';
import { DeleteForm } from '../forms/delete.form';
import { AdminRelationshipsEditor } from './admin-relationships-editor.component';
import { RelationshipSearchPipe } from '../../pipes/relationship-search.pipe';


@Component({
    selector: 'd3s-admin-relationships-component',
    directives: [DataTable, Column, TileActionsComponent, PredicatesTile, FieldDefinitionTile, DeleteForm, AdminRelationshipsEditor, Button],
    providers: [RelationshipsService],
    pipes: [RelationshipSearchPipe],
    template: `<div class="row">
                    <div class="col l6 s12">                    
                        <div class="tile tile-detail">
                            <header *ngIf="!showEditor && !showDelete">Relationship Types
                                <d3s-tile-actions [hasAdd]="true" [addTitle]="'Add Relationship'" (addClick)="add()"></d3s-tile-actions>                            
                            </header>    
                            <div *ngIf="isLoading">
                                <div style="padding:10px;text-align:center;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                            </div>    
                            <div  *ngIf="!showEditor && !showDelete && !isLoading" class="row">
                                <div class="col l10 s12">                                                                         
                                    <input type="text" [(ngModel)]="searchValue" placeholder="Search Relationships" style="width: 100%;">
                                </div>
                                <div class="col l2 s12">                                                                         
                                    <button [disabled]="!searchValue" pButton type="button" (click)="searchValue='';" label="Clear" style="width: 100%;"></button>
                                </div>
                                <div class="col s12">
                                    <p-dataTable [value]="relationships | relationshipSearch: searchValue" selectionMode="single" [rows]="20" [paginator]="true" [pageLinks]="3" expandableRows="true" [(selection)]="selected"  (onRowDblclick)="selected=$event.data;showEditor=true;" >                                                                                        
                                        <p-column field="Source" header="Side 1 Type" [sortable]="true"></p-column>                                
                                        <p-column field="SourceName" header="Side 1 Name" [sortable]="true"></p-column>
                                        <p-column field="Target" header="Side 2 Type" [sortable]="true"></p-column>                                
                                        <p-column field="TargetName" header="Side 2 Name" [sortable]="true"></p-column>
                                        <p-column [style]="{width:'40px'}">
                                            <template let-relationship="rowData">
                                                <div class="RowTools">
                                                    <a style="cursor:pointer;" (click)="selected=relationship;showEditor=true"><i class="fa fa-pencil"></i></a>                                        
                                                </div>
                                            </template>
                                        </p-column>                            
                                        <p-column  [style]="{width:'40px'}">
                                            <template let-relationship="rowData">
                                                <div class="RowTools">                                
                                                    <a style="cursor:pointer;" (click)="selected=relationship;showDelete=true"><i class="fa fa-trash-o"></i></a>                                    
                                                </div>
                                            </template>
                                        </p-column>    
                                    </p-dataTable>  
                                </div>
                            </div>
                            <delete-form *ngIf="showDelete"
                                [callback]="theDeleteCallback"
                                [itemId]="selected?.ID"
                                [method]="'callback'"
                                [prompt]="'Are you sure you want to delete the relationship [' + [selected?.SourceName] + ' / ' + [selected?.TargetName]  + ']?'"                                         
                                (onCancel)="showDelete=false;"
                            ></delete-form>  
                            <d3s-admin-relationships-editor *ngIf="showEditor" [relationshipID]="selected?.ID" (saveClick)="saveRelationship($event)" (closeClick)="closeEditor()"></d3s-admin-relationships-editor>       
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
    theDeleteCallback: Function;
    headerRows: any[];
    searchValue: string = "";

    constructor(private relationshipsService: RelationshipsService, protected messagesService: MessagesService, headerBreadcrumbService: HeaderBreadcrumbService, pageHeader: PageHeader) {
        super(headerBreadcrumbService, pageHeader);
        this.areaDescription = "Create the possibility of establishing relationships between different objects within the system.";
        this.areaName = "Relationship Types";
        this.setCommonItems();
        this.theDeleteCallback = this.deleteRelationship.bind(this);
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

    findRelationshipIndex(id: number) {
        var index: number = -1;
        for (var relationship of this.relationships) {
            index++;
            if (relationship.ID == id) return index;
        }
    }

    deleteRelationship(id : number) {
        this.relationshipsService.deleteRelationship(id);
        this.showDelete = false;
        this.selected = this.relationships.length > 0 ? this.relationships[0] : null;
        this.relationships.splice(this.findRelationshipIndex(id), 1);
    }

    saveRelationship(event) {
        this.relationshipsService.saveRelationship(event.relationship)
            .then(result => {
                this.getRelationships(); // reload relationship detail and relationship models are incompatible               
                this.showEditor = false;
            });
    }

    closeEditor() {
        this.showEditor = false;
        if (this.selected == null) {
            this.selected = this.relationships.length > 0 ? this.relationships[0] : null;
        }
    }

    add() {
        this.showEditor = true;
        this.selected = null;
    }
}