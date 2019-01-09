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
                <span *ngIf="!isLoading && relations.length > 0 && !shouldShowEditor() && !showTechnical && !showDelete">                    
                    <input type="text" [hidden]="!simpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..." class="grid-simple-filter">
                    <p-table #dt 
                        [value]="relations" 
                        selectionMode="single" 
                        [metaKeySelection]="true" 
                        [globalFilterFields]="globalFilterFields" 
                        [pageLinks]="3" 
                        [paginator]="true" 
                        [rows]="defaultInitialItemsPerPage" 
                        [rowsPerPageOptions]="defaultPagingOptions" 
                        [(selection)]="selected"
                        [scrollable]="true"
                        scrollWidth="100%">
                        <ng-template pTemplate="colgroup" >
                            <colgroup>
                                <col style="width: 28px">
                                <col style="width: 28px">
                                <col style="width: 28px">
                                <col *ngFor="let col of columns" style="width: 250px">
                            </colgroup>
                        </ng-template>  
                        <ng-template pTemplate="header">
                            <tr>
                                <th></th>
                                <th></th>
                                <th></th>
                                <th *ngFor="let column of columns" [pSortableColumn]="column.sortable ? column.datafield : null " style="width: 250px">
                                    {{column.text}}
                                    <d3s-sortIcon [field]="column.datafield"></d3s-sortIcon>
                                </th>
                            </tr>
                            <tr [hidden]="simpleFilter">
                                <th></th>
                                <th></th>
                                <th></th>
                                <th *ngFor="let column of columns">
                                    <d3s-column-filter *ngIf="column.filterable" [field]="column.datafield" [datatype]="'text'"></d3s-column-filter>
                                </th>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="body" let-item>
                            <tr (dblclick)="selected=item;showEditor=true;" [pSelectableRow]="item">
                                <td>
                                    <div class="RowTools" *ngIf="hasEdit && !readOnly">
                                        <a style="cursor:pointer;" (click)="selected=item;showEditor=true;" title="Edit"><i class="fa fa-pencil"></i></a>
                                    </div>
                                </td>
                                <td>
                                    <div class="RowTools" *ngIf="hasDelete && !readOnly">
                                        <a style="cursor:pointer;" (click)="selected=item;doDelete();" title="Remove"><i class="fa fa-trash-o"></i></a>
                                    </div>
                                </td>
                                <td>
                                    <div class="RowTools" [ngClass]="{'RowTools': item.HasTechnicalRelationships, 'InActiveRowTools': !item.HasTechnicalRelationships}">
                                        <a style="cursor:pointer;" (click)="selected=item;showTechnical=true;" title="Technical Relationships"><i class="fa fa-bolt"></i></a>
                                    </div>
                                </td>
                                <td *ngFor="let column of columns">
                                    <d3s-dynamic-field-value *ngIf="column.text != 'Name';else nameField" [column]="column" [fields]="fields" [item]="item"></d3s-dynamic-field-value>
                                    <ng-template #nameField>
                                        <d3s-preview-tooltip [objectType]="item.Object" [objectId]="item.ObjectID" (click)="selectObject(item)">{{item.Name}}</d3s-preview-tooltip>
                                    </ng-template>
                                </td>
                            </tr>
                        </ng-template>
                        <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                            <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                        </ng-template>
                    </p-table>
                </span>
                <div *ngIf="showTechnical && !shouldShowEditor()">
                    <d3s-relationship-technical-relations [objectName]="objectName" [relationship]="selected" [addTechnicalRelationship]="addRelationship" (allTechnicalRelationshipsDeleted)="selected.HasTechnicalRelationships=false;" (addTechnicalRelationshipChange)="addRelationship=false;addRelationshipChange.emit(addRelationship);selected.HasTechnicalRelationships=true;" (closeClick)="showTechnical=false" [hasEdit]="hasEdit" [hasDelete]="hasDelete"></d3s-relationship-technical-relations>                    
                </div>
                <d3s-dynamic-editor *ngIf="shouldShowEditor()"  [createUri]="'form/dynamicedit/create/intersect/'" [editUri]="'form/dynamicedit/edit/intersect/'" [objectID]="intersectTypeID" [objectType]="'IntersectType'" [targetType]="objectType" [targetTypeID]="objectID" [title]="targetName + ' Relationship'" [selection]="addRelationship ? null : selected" [rowID]="'ID'" (saveClick)="saveRelationship($event)" (closeClick)="closeEditor()"></d3s-dynamic-editor>                
                <div *ngIf="!isLoading && relations.length == 0 && !shouldShowEditor()">
                    <h5 class="center-align" style="font-weight:bold;">No relationships exist from this object to this object type.  Use the plus link in the upper right of this tile to setup new relationships.</h5>                    
                </div>                                                   
                <d3s-delete-form *ngIf="showDelete"
                    [callback]="theDeleteCallback"
                    [itemId]="selected?.ID"
                    [method]="'callback'"
                    [prompt]="'Are you sure you want to delete the relationship [' + [selected?.Name] + ']?'"                                         
                    (onCancel)="cancelDelete();"
                ></d3s-delete-form>   
                
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
    @Input() readOnly: boolean = false;

    @Output() readOnlyChange = new EventEmitter();
    @Output() addRelationshipChange = new EventEmitter();
    @Output() relationshipAdded = new EventEmitter();
    @Output() relationshipRemoved = new EventEmitter();
    @Output() deleteOn = new EventEmitter();
    @Output() deleteOff = new EventEmitter();
    @Output() onFilterChange = new EventEmitter();
   
    @Input() simpleFilter: boolean;

    private fields: GridField[] = [];

    get globalFilterFields(): string[] {
        return this.columns.map(c => c.datafield);
    }

    get taxonomyName() {
        return CompanySettings.ArtifactType_TaxonomyTypeID || '';
    }

    relations: any[] = [];
    columns: GridColumn[] = [];
    
    selected: any = null;
    showEditor: boolean = false;
    showDelete: boolean = false;
    theDeleteCallback: Function;

    private showTechnical: boolean = false;

    @ViewChild('dt') datatable;
    
    constructor(private router: Router, private gridDefinitionService: GridDefinitionService, protected relationshipsService: RelationshipsService, private messagesService: MessagesService) {
        super();
        this.theDeleteCallback = this.deleteItem.bind(this);
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

        this.gridDefinitionService.getGridDefinition(this.intersectTypeID, 'IntersectType', this.targetTypeID, this.targetType)
            .then(result => {
                this.columns = result.Columns;
                this.fields = result.Fields;
                this.readOnly = result.IsReadOnly;
                this.readOnlyChange.emit(this.readOnly);
                if (result.Fields.findIndex(x => x.name == 'TaxonomyType') >= 0) {
                    this.columns.unshift({
                        text: this.taxonomyName,
                        cellsformat: '',
                        datafield: 'TaxonomyType',
                        type: 'string',
                        description: null,
                        columnWidth: null
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
        this.relationshipsService.deleteRelationshipItem(item).then(res => {
            this.relations = this.relations.filter(x => x.ID != item);
            this.relationshipRemoved.emit();
        });
        this.deleteOff.emit();
        this.showDelete = false;
    }

    doDelete() {
        this.deleteOn.emit();
        this.showDelete = true;
    }

    cancelDelete() {
        this.deleteOff.emit();
        this.showDelete = false;
    }
    selectObject(item) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl(item.Object, item.ObjectID, item.TypeID));
    }
    onFilter(event:any) {

        let count = 0;
        let qstring: string="";
        
        for (var key in event.filters) {
            var matchcondition: string = event.filters[key].matchMode == "startsWith" ? "STARTS_WITH" : event.filters[key].matchMode;
            qstring += `&filterdatafield${count}=${key}&filtercondition${count}=${matchcondition}&filtervalue${count}=${event.filters[key].value}`;
            count++;
        } 
        qstring += '&filterscount=' + count;
        this.onFilterChange.emit(qstring);
    
    }
  
}