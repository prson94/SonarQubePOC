///<reference path="../../es6-shim.d.ts"/>
import { Component, Input, Output, OnChanges, SimpleChange, EventEmitter, ViewChild} from '@angular/core';
import { Lookup, LookupItem } from '../../models/lookup.model';
import { GridDefinition, GridColumn } from '../../models/grid-definition.model';
import { MessagesService, GridDefinitionService, RelationshipsService} from '../../services/index';
import { BaseComponent } from '../shared/base.component';

@Component({
    selector: 'd3s-dynamic-relationship-grid',    
    providers: [GridDefinitionService, RelationshipsService],
    template: `                   
                <div *ngIf="isLoading" style="width:100%; text-align:center;">
                    <div style="padding:10px;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                </div>     
                <span *ngIf="!isLoading && relations.length > 0 && !shouldShowEditor() && !showTechnical">                    
                    <input #gb type="text" pInputText size="100" placeholder="Search..." style="margin-bottom:10px;width:100%;">                                              
                    <p-dataTable #dt [globalFilter]="gb"  scrollable="true" scrollWidth="100%" [rowsPerPageOptions]="[5,10,20]" [value]="relations" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" expandableRows="true" (onRowDblclick)="selected=$event.data;showEditor=true;" [(selection)]="selected" >                                                                                                  
                        <p-column field="Name" header="Name" [sortable]="true" [style]="{'width':'250px'}">
                            <template let-item="rowData" pTemplate type="body">
                                <d3s-tooltip [objectType]="item.Object" [objectId]="item.ObjectID" tooltipType="preview">{{item.Name}}</d3s-tooltip>
                            </template> 
                        </p-column>                                                                                                              
                                   
                        <p-column  [style]="{width:'28px'}">
                                <template let-item="rowData" pTemplate type="body">
                                    <div class="RowTools">                                
                                        <a style="cursor:pointer;" (click)="selected=item;showEditor=true;" title="Edit"><i class="fa fa-pencil"></i></a>                                                                           
                                    </div>
                                </template>
                        </p-column>                   
                        <p-column  [style]="{width:'28px'}">
                                <template let-item="rowData" pTemplate type="body">
                                    <div class="RowTools">                                                    
                                        <a style="cursor:pointer;" (click)="selected=item;deleteItem(item);" title="Remove"><i class="fa fa-trash-o"></i></a>                                    
                                    </div>
                                </template>
                        </p-column>           
                        <p-column header="Classification" field="ClassificationText" [sortable]="true" [style]="{'width':'150px'}"></p-column>    
                        <p-column *ngFor="let column of columns" [field]="column.datafield" [header]="column.text" [sortable]="column.sortable" [style]="{'width':'250px'}"></p-column>        
                    </p-dataTable>   
                </span>
                <div *ngIf="showTechnical">
                    Technical Relationships
                </div>
                <d3s-dynamic-editor *ngIf="shouldShowEditor()"  [createUri]="'form/dynamicedit/create/intersect/'" [editUri]="'form/dynamicedit/edit/intersect/'" [objectID]="intersectTypeID" [objectType]="'IntersectType'" [targetType]="objectType" [targetTypeID]="objectID" [title]="'Relationship'" [selection]="addRelationship ? null : selected" [rowID]="'ID'" (saveClick)="saveRelationship($event)" (closeClick)="closeEditor()"></d3s-dynamic-editor>                
                <div *ngIf="!isLoading && relations.length == 0 && !shouldShowEditor()">
                    <h5 class="center-align" style="font-weight:bold;">No relationships exist from this object to this object type.  Use the plus link in the upper left of this tile to setup new relationships.</h5>                    
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
    private showTechnical: boolean = false;

    @ViewChild('dt') datatable;
    

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
                for (let rel of result) {
                    rel.ClassificationText = rel.Classification == 1 ? "Critical" : "Normal";
                }
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

    public export() {
        if (this.datatable)
            this.datatable.exportCSV();
    }

    closeEditor() {
        this.showEditor = false;
        if (this.addRelationship) {
            this.addRelationship = !this.addRelationship;
            this.addRelationshipChange.emit(this.addRelationship);
        }
    }    

    saveRelationship(event) {        
        if (event.item.id != undefined && event.item.id == 0) {
            let count = 1;
            if (event.values && event.values.Items) {                
                count = event.values.Items.split(',').length;
            }
            this.relationshipAdded.emit({ count: count });
        }

        this.getData();        
        this.closeEditor();
    }

    deleteItem(item) {
        this.relationshipsService.deleteRelationshipItem(item.ID);

        this.relations.splice(this.findItemIndex(item.ID), 1);

        this.relationshipRemoved.emit();
    }
    
}


