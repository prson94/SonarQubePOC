///<reference path="../../es6-shim.d.ts"/>
import { Component, Input, Output, OnChanges, SimpleChange, EventEmitter} from '@angular/core';
import {DataTable, Column} from 'primeng/primeng';
import { Lookup, LookupItem } from '../../models/lookup.model';
import { GridDefinition, GridColumn } from '../../models/grid-definition.model';
import { MessagesService, GridDefinitionService, RelationshipsService} from '../../services/index';
import { BaseComponent } from '../shared/base.component';
import { ClassificationTypePipe} from '../../pipes/classification-display.pipe';
import { DynamicEditorComponent } from './dynamic-editor.component';


@Component({
    selector: 'd3s-dynamic-relationship-grid',
    directives: [DataTable, Column, DynamicEditorComponent],
    pipes: [ClassificationTypePipe],
    providers: [GridDefinitionService, RelationshipsService],
    template: `                   
                <div *ngIf="isLoading" style="width:100%; text-align:center;">
                    <div style="padding:10px;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                </div>           
               <p-dataTable *ngIf="!isLoading && relations.length > 0 && !shouldShowEditor()" scrollable="true" scrollWidth="100%" [rowsPerPageOptions]="[5,10,20]" [value]="relations" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" expandableRows="true" (onRowDblclick)="selected=$event.data;showEditor=true;" [(selection)]="selected" >                                                                                                  
                    <p-column field="Name" header="Name" [filter]="true" [sortable]="true" [style]="{'width':'250px'}"></p-column>
                    <p-column header="Classification" field="Classification" [filter]="true" [sortable]="true" [style]="{'width':'150px'}">                        
                        <template let-col let-rowTenant="rowData">
                            <span>{{rowTenant?.Classification | classificationTypeDisplayValue}}</span>
                        </template>
                    </p-column>           
                    <p-column *ngFor="let column of columns" [field]="column.datafield" [header]="column.text" [filter]="column.filterable" [sortable]="column.sortable" [style]="{'width':'250px'}"></p-column>                    
                </p-dataTable>   
                <d3s-dynamic-editor *ngIf="shouldShowEditor()"  [createUri]="'form/dynamicedit/create/intersect/'" [editUri]="'form/dynamicedit/edit/intersect/'" [objectID]="intersectTypeID" [objectType]="'IntersectType'" [targetType]="objectType" [targetTypeID]="objectID" [title]="'Relationship'" [selection]="addRelationship ? null : selected" [rowID]="'ID'" (saveClick)="saveRelationship($event)" (closeClick)="closeEditor()"></d3s-dynamic-editor>                
                <div class="row" *ngIf="!isLoading && relations.length == 0 && !shouldShowEditor()">
                        <div class="col s12">
                            <span class="center">No relationships exist to this object type.  Use the plus link in the upper left of this tile to setup new relationships.</span>
                        </div>
                </div>
                `
})

export class DynamicRelationshipGridComponent extends BaseComponent implements OnChanges {
    @Input() objectType: string;
    @Input() objectID: number;
    @Input() targetType: string;
    @Input() targetTypeID: number;
    @Input() intersectTypeID: number;
    @Input() addRelationship: boolean;

    @Output() addRelationshipChange = new EventEmitter();
    @Output() relationshipAdded = new EventEmitter();
    @Output() relationshipRemoved = new EventEmitter();

    relations: any[] = [];
    columns: GridColumn[] = [];
    
    selected: any = null;
    showEditor: boolean = false;
    
    

    constructor(private gridDefinitionService: GridDefinitionService, protected relationshipsService: RelationshipsService) {
        super();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        
        if (this.objectID != null && this.objectType != null && this.targetType != null && this.targetTypeID != null && this.intersectTypeID != null) this.load();                
    }

    load() {
        this.getFieldsDefinition();
        this.getData();
    }
    

    getFieldsDefinition() {
        this.gridDefinitionService.getGridDefinition(this.intersectTypeID, 'IntersectType')
            .then(result => {
                this.columns = result.Columns;
            });
    }

    getData() {
        this.isLoading = true;
        this.relationshipsService.getObjectRelationships(this.objectType, this.objectID, this.targetType, this.targetTypeID, this.intersectTypeID)
            .then(result => {
                this.relations = result;
                this.isLoading = false;
                if (this.relations.length > 0) this.selected = this.relations[0];                
            });
    }
    
    private findItemIndex(id: number) {
        var index: number = -1;
        for (var item of this.relations) {
            index++;
            if (item.ID == id) return index;
        }
    }

    private shouldShowEditor(): boolean {
        return this.addRelationship || this.showEditor;
    }

    closeEditor() {
        this.showEditor = false;
        if (this.addRelationship) {
            this.addRelationship = !this.addRelationship;
            this.addRelationshipChange.emit(this.addRelationship);
        }
    }    

    saveRelationship(event) {        
        if (event.item.id == undefined)
            this.relationshipAdded.emit();

        this.getData();        
        this.closeEditor();
    }
    
}


