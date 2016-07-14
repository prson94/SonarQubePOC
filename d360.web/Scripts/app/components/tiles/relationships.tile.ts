///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, Output, EventEmitter, OnChanges, SimpleChange } from '@angular/core';
import {DataTable, Column, Button} from 'primeng/primeng';
import { RelationshipsService  } from '../../services/index';
import { TileActionsComponent } from './tile-actions.component';
import { Relationship } from '../../models/relationship.model';
import { DeleteForm } from '../forms/delete.form';
import { AdminRelationshipsEditor } from '../admin/admin-relationships-editor.component';
import { RelationshipSearchPipe } from '../../pipes/relationship-search.pipe';


@Component({
    selector: 'd3s-relationships-tile',
    directives: [DataTable, Column, TileActionsComponent, DeleteForm, AdminRelationshipsEditor, Button],
    providers: [RelationshipsService],
    pipes: [RelationshipSearchPipe],
    template: `
                <header *ngIf="!showEditor && !showDelete">Relationship Types
                    <d3s-tile-actions [hasAdd]="true" [addTitle]="'Add Relationship'" (addClick)="add()"></d3s-tile-actions>                            
                </header>    
                <div *ngIf="isLoading">
                    <div style="padding:10px;text-align:center;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                </div>    
                <div  *ngIf="!showEditor && !showDelete && !isLoading" class="row">
                    <div *ngIf="showFilter" class="col l10 m9 s12">                                                                         
                        <input type="text" [(ngModel)]="searchValue" placeholder="Search Relationships" style="width: 100%;">
                    </div>
                    <div *ngIf="showFilter" class="col l2 m3 s12">                                                                         
                        <button [disabled]="!searchValue" pButton type="button" (click)="searchValue='';" label="Clear" style="width: 100%;"></button>
                    </div>
                    <div class="col s12">
                        <p-dataTable [value]="relationships | relationshipSearch: searchValue" selectionMode="single" [rows]="20" [paginator]="true" [pageLinks]="3" expandableRows="true" [(selection)]="selected" (onRowSelect)="onSelectedChanged.emit($event.data)"  (onRowDblclick)="selected=$event.data;onSelectedChanged.emit($event.data);showEditor=true;" >
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
            `    
})

export class RelationshipsTile implements OnChanges {
    relationships: Relationship[] = [];

    selected: Relationship;

    @Input() filterToName: string;
    @Output() onSelectedChanged = new EventEmitter();

    showEditor: boolean = false;
    showDelete: boolean = false;
    theDeleteCallback: Function;
    @Input() showFilter: boolean = true;
    
    searchValue: string = "";
    isLoading: boolean = false;

    constructor(private relationshipsService: RelationshipsService) {        
        this.theDeleteCallback = this.deleteRelationship.bind(this);
    }

    ngOnInit() {
        this.getRelationships();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {            
            if (p == 'filterToName') {
                this.searchValue = changes['filterToName'].currentValue;                
            }
        }        
    }

    getRelationships() {
        this.isLoading = true;
        this.relationshipsService.getRelations()
            .then(result => {
                this.relationships = result;
                this.isLoading = false;
                if (this.relationships.length > 0) {
                    this.selected = this.relationships[0];    
                    this.onSelectedChanged.emit(this.selected)                
                }
            });
    }

    findRelationshipIndex(id: number) {
        var index: number = -1;
        for (var relationship of this.relationships) {
            index++;
            if (relationship.ID == id) return index;
        }
    }

    deleteRelationship(id: number) {
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
