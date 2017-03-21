import { Component, Input, Output, OnChanges, SimpleChange, EventEmitter, ViewChild} from '@angular/core';
import { Router } from '@angular/router';
import { Lookup, LookupItem } from '../../../models/lookup.model';
import { GridDefinition, GridColumn, GridField } from '../../../models/grid-definition.model';
import { MessagesService } from '../../../services/messages.service';
import { GridDefinitionService } from '../../../services/grid-definition.service';
import { RelationshipsService} from '../../../services/relationships.service';
import { BaseComponent } from '../base.component';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';

declare var CompanySettings;

@Component({
    selector: 'd3s-dynamic-relationship-grid',    
    providers: [GridDefinitionService, RelationshipsService],
    template: `                   
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <span *ngIf="!isLoading && relations.length > 0 && !shouldShowEditor() && !showTechnical">                    
                    <input #gb [hidden]="!simpleFilter" type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">                                              
                    <p-dataTable #dt [globalFilter]="gb"  scrollable="true" scrollWidth="100%" [rowsPerPageOptions]="defaultPagingOptions" [value]="relations" selectionMode="single" [rows]="defaultInitialItemsPerPage" paginator="true" pageLinks="3" (onRowDblclick)="selected=$event.data;showEditor=true;" [(selection)]="selected" >                                                                                                  
                        <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                        <p-column  [style]="{width:'28px'}">
                                <template let-item="rowData" pTemplate type="body">
                                    <div class="RowTools" *ngIf="hasEdit">                                
                                        <a style="cursor:pointer;" (click)="selected=item;showEditor=true;" title="Edit"><i class="fa fa-pencil"></i></a>                                                                           
                                    </div>
                                </template>
                        </p-column>                   
                        <p-column  [style]="{width:'28px'}">
                                <template let-item="rowData" pTemplate type="body">
                                    <div class="RowTools" *ngIf="hasDelete">                                                    
                                        <a style="cursor:pointer;" (click)="selected=item;deleteItem(item);" title="Remove"><i class="fa fa-trash-o"></i></a>                                    
                                    </div>
                                </template>
                        </p-column>           
                        <p-column  [style]="{width:'28px'}">
                                <template let-item="rowData" pTemplate type="body">
                                    <div class="RowTools" [ngClass]="{'RowTools': item.HasTechnicalRelationships, 'InActiveRowTools': !item.HasTechnicalRelationships}">                                
                                        <a style="cursor:pointer;" (click)="selected=item;showTechnical=true;" title="Technical Relationships"><i class="fa fa-bolt"></i></a>                                                                           
                                    </div>
                                </template>
                        </p-column>   
                        <p-column field="Name" header="Name" sortable="true" [style]="{'width':'250px'}" [filter]="!simpleFilter" >
                            <template let-item="rowData" pTemplate type="body">
                                <d3s-tooltip [objectType]="item.Object" [objectId]="item.ObjectID" tooltipType="preview"><a (click)="selectObject(item)">{{item.Name}}</a></d3s-tooltip>
                            </template> 
                        </p-column>
                        <p-column *ngFor="let column of columns" [field]="column.datafield" [header]="column.text" sortable="true" [style]="{'width':'250px'}"  [filter]="!simpleFilter">
                            <template let-item="rowData" pTemplate type="body">
                                    <d3s-dynamic-field-value [column]="column" [fields]="fields" [item]="item"></d3s-dynamic-field-value>                                 
                            </template>
                        </p-column>        
                        <p-column></p-column>
                    </p-dataTable>   
                </span>
                <div *ngIf="showTechnical && !shouldShowEditor()">
                    <d3s-relationship-technical-relations [objectName]="objectName" [relationship]="selected" [addTechnicalRelationship]="addRelationship" (addTechnicalRelationshipChange)="addRelationship=false;addRelationshipChange.emit(addRelationship);" (closeClick)="showTechnical=false" [hasEdit]="hasEdit" [hasDelete]="hasDelete"></d3s-relationship-technical-relations>                    
                </div>
                <d3s-dynamic-editor *ngIf="shouldShowEditor()"  [createUri]="'form/dynamicedit/create/intersect/'" [editUri]="'form/dynamicedit/edit/intersect/'" [objectID]="intersectTypeID" [objectType]="'IntersectType'" [targetType]="objectType" [targetTypeID]="objectID" [title]="targetName + ' Relationship'" [selection]="addRelationship ? null : selected" [rowID]="'ID'" (saveClick)="saveRelationship($event)" (closeClick)="closeEditor()"></d3s-dynamic-editor>                
                <div *ngIf="!isLoading && relations.length == 0 && !shouldShowEditor()">
                    <h5 class="center-align" style="font-weight:bold;">No relationships exist from this object to this object type.  Use the plus link in the upper left of this tile to setup new relationships.</h5>                    
                </div>                                                   
                
                `
})

export class DynamicRelationshipGridComponent extends BaseComponent implements OnChanges {
    @Input() objectType: string;
    @Input() objectID: number;
    @Input() objectName: string;
    @Input() targetType: string;
    @Input() targetTypeID: number;
    @Input() targetName: string;
    @Input() intersectTypeID: number;
    @Input() addRelationship: boolean;
    @Input() hasEdit: boolean = true;
    @Input() hasDelete: boolean = true;

    @Output() addRelationshipChange = new EventEmitter();
    @Output() relationshipAdded = new EventEmitter();
    @Output() relationshipRemoved = new EventEmitter();


    @Input() simpleFilter: boolean;

    private fields: GridField[] = [];

    get taxonomyName() {
        return CompanySettings.ArtifactType_TaxonomyTypeID || '';
    }

    
    relations: any[] = [];
    columns: GridColumn[] = [];
    
    selected: any = null;
    showEditor: boolean = false;
    private showTechnical: boolean = false;

    @ViewChild('dt') datatable;
    

    constructor(private router: Router, private gridDefinitionService: GridDefinitionService, protected relationshipsService: RelationshipsService) {
        super();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {        
        if ((changes['objectID'] || changes['objectType'] || changes['intersectTypeID'] || changes['targetTypeID']) && (this.objectID != null && this.objectType != null && this.targetType != null && this.targetTypeID != null && this.intersectTypeID != null)) {
            this.load();
            this.showTechnical = false;            
        }
    }

    load() {
        this.getFieldsDefinition();
        this.getData();
    }
    

    getFieldsDefinition() {
        this.gridDefinitionService.getGridDefinition(this.intersectTypeID, 'IntersectType')
            .then(result => {
                this.columns = result.Columns;
                this.fields = result.Fields;
                if (result.Fields.findIndex(x => x.name == 'TaxonomyType') >= 0) {
                    this.columns.unshift({
                        text: this.taxonomyName,
                        cellsformat: '',
                        datafield: 'TaxonomyType',
                        type: 'string',
                        description: null
                    });
                }
            });
    }

    getData() { 
        this.isLoading = true;
        this.relationshipsService.getObjectRelationships(this.objectType, this.objectID, this.targetType, this.targetTypeID, this.intersectTypeID)
            .then(result => {
                this.relations = result;
                this.isLoading = false;
                if (this.relations.length > 0) this.selected = this.relations[0]; 
                if (this.shouldShowEditor()) this.closeEditor();               
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
        return (this.addRelationship || this.showEditor) && !this.showTechnical;
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
        this.relationshipsService.deleteRelationshipItem(item.ID).then(res => {
            this.relations.splice(this.findItemIndex(item.ID), 1);

            this.relationshipRemoved.emit();
        });
    }

    selectObject(item) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl(item.Object, item.ObjectID, item.TypeID));
    }
}


