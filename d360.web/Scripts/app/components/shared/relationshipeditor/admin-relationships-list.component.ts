import { Input, Component, Output, EventEmitter, OnChanges, SimpleChange } from '@angular/core';
import { RelationshipsService } from '../../../services/relationships.service';
import { MessagesService  } from '../../../services/messages.service';
import { RelationshipType } from '../../../models/relationship.model';
import { BaseComponent } from '../../shared/base.component';

@Component({
    selector: 'd3s-admin-relationships-list',
    providers: [RelationshipsService],    
    template: `
                <header *ngIf="!showEditor && !showDelete">Relationship Types
                    <d3s-tile-actions [hasAdd]="true" (addClick)="add()" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter" [hasExport]="true" (exportClick)="export()"></d3s-tile-actions>
                </header>    
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div  *ngIf="!showEditor && !showDelete && !isLoading" class="row">                    
                    <div class="col s12">
                        <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..." class="grid-simple-filter">
                        <p-table #dt [value]="relationships" selectionMode="single" [metaKeySelection]="true" [globalFilterFields]="['ID','SubjectTypeName','PredicateName','ObjectTypeName']" [pageLinks]="3" [paginator]="true" [rows]="20"  [selection]="selected" (selectionChange)="selected=$event;selectedChange.emit(selected)">
                            <ng-template pTemplate="header">
                                <tr>
                                    <th [pSortableColumn]="'ID'" style="width: 10%;">
                                        ID
                                        <d3s-sortIcon [field]="'ID'"></d3s-sortIcon>
                                    </th>
                                    <th [pSortableColumn]="'SubjectTypeName'">
                                        Subject
                                        <d3s-sortIcon [field]="'SubjectTypeName'"></d3s-sortIcon>
                                    </th>
                                    <th [pSortableColumn]="'PredicateName'">
                                        Predicate
                                        <d3s-sortIcon [field]="'PredicateName'"></d3s-sortIcon>
                                    </th>
                                    <th [pSortableColumn]="'ObjectTypeName'">
                                        Object
                                        <d3s-sortIcon [field]="'ObjectTypeName'"></d3s-sortIcon>
                                    </th>
                                    <th style="width: 40px"></th>
                                    <th style="width: 40px"></th>
                                    <th style="width: 40px"></th>
                                    <th style="width: 40px"></th>
                                </tr>
                                <tr [hidden]="showSimpleFilter">
                                    <th><d3s-column-filter [field]="'ID'" [datatype]="'text'"></d3s-column-filter></th>
                                    <th><d3s-column-filter [field]="'SubjectTypeName'" [datatype]="'text'"></d3s-column-filter></th>
                                    <th><d3s-column-filter [field]="'PredicateName'" [datatype]="'text'"></d3s-column-filter></th>
                                    <th><d3s-column-filter [field]="'ObjectTypeName'" [datatype]="'text'"></d3s-column-filter></th>
                                    <th></th>
                                    <th></th>
                                    <th></th>
                                    <th></th>
                                </tr>
                            </ng-template>
                            <ng-template pTemplate="body" let-item>
                                <tr (dblclick)="selected=item;selectedChange.emit(selected);showEditor=true;" [pSelectableRow]="item">
                                    <td>{{item.ID}}</td>
                                    <td>
                                        <span>{{item?.SubjectTypeName}}<span style="color: #999;font-size:75%;"> ({{displayTypeName(item?.SubjectClass.Name)}})</span></span>
                                    </td>
                                    <td>
                                        <span *ngIf="item.PredicateName && item.PredicateInverse">{{item.PredicateName}} / {{item.PredicateInverse}}</span>
                                    </td>
                                    <td>
                                        <span>{{item?.ObjectTypeName}}<span style="color: #999;font-size:75%;"> ({{displayTypeName(item?.ObjectClass.Name)}})</span></span>
                                    </td>
                                    <td class="RowTools">
                                        <d3s-preview-tooltip objectType="IntersectType" [objectId]="item.ID" icon="info"></d3s-preview-tooltip>
                                    </td>
                                    <td>
                                        <div class="RowTools">
                                            <a style="cursor:pointer;" title="Download all relationships in this type" (click)="downloadRel(item)"><i class="fa fa-download"></i></a>
                                        </div>
                                    </td>
                                    <td>
                                        <div *ngIf="!item.IsSystem" class="RowTools">
                                            <a style="cursor:pointer;" (click)="selected=item;selectedChange.emit(selected);showEditor=true"><i class="fa fa-pencil"></i></a>
                                        </div>
                                    </td>
                                    <td>
                                        <div *ngIf="!item.IsSystem" class="RowTools">
                                            <a style="cursor:pointer;" (click)="selected=item;selectedChange.emit(selected);showDelete=true"><i class="fa fa-trash-o"></i></a>
                                        </div>
                                    </td>
                                </tr>
                            </ng-template>
                            <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                                <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                            </ng-template>
                        </p-table>
                    </div>
                </div>
                <d3s-delete-form *ngIf="showDelete"
                    [callback]="theDeleteCallback"
                    [itemId]="selected?.ID"
                    [method]="'callback'"
                    [prompt]="'Are you sure you want to delete the relationship [' + [selected?.SubjectTypeName] + ' / ' + [selected?.ObjectTypeName]  + ']?'"
                    (onCancel)="showDelete=false;"
                ></d3s-delete-form>  
                <d3s-admin-relationships-editor *ngIf="showEditor" [relationshipID]="selected?.ID" (saveClick)="saveRelationship($event)" (closeClick)="closeEditor()"></d3s-admin-relationships-editor>
            `    
})

export class AdminRelationshipsListComponent extends BaseComponent implements OnChanges {
    relationships: RelationshipType[] = [];
    
    @Input() filterToName: string;

    @Input() objectType: string;
    @Input() objectID: number;
    
    @Input() selected: RelationshipType;
    @Output() selectedChange = new EventEmitter();

    showEditor: boolean = false;
    showDelete: boolean = false;
    theDeleteCallback: Function;
    
    constructor(private messagesService: MessagesService, private relationshipsService: RelationshipsService) {   
        super();     
        this.theDeleteCallback = this.deleteRelationship.bind(this);
    }

    ngOnInit() {        
        this.getRelationships();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {        
        if (changes['filterToName'] && changes['filterToName'].currentValue != changes['filterToName'].previousValue) {
            this.getRelationships();
        }
    }

    private export() {
        this.relationshipsService.exportRelationshipTypes();
    }

    private filterResults() {
        if (this.filterToName && this.filterToName.length > 0) {
            var search = this.filterToName.toLowerCase();
            this.relationships = this.relationships.filter(item =>
                item.Object && item.Object.toLowerCase().includes(search) ||
                item.Subject && item.Subject.toLowerCase().includes(search) ||
                item.ObjectTypeName && item.ObjectTypeName.toLowerCase().includes(search) ||
                item.SubjectTypeName && item.SubjectTypeName.toLowerCase().includes(search)
            );
        }
    }

    getRelationships() {
        this.isLoading = true;
        if (this.objectID && this.objectType) {
            console.log("new method");
            this.relationshipsService.getRelationshipTypesById(this.objectID, this.objectType)
                .then(result => {
                    this.relationships = result;
                    this.isLoading = false;
                    if (this.relationships.length > 0) {
                        this.selected = this.relationships[0];
                        this.selectedChange.emit(this.selected)
                    }
                });
        } else {
            console.log("old method");
            this.relationshipsService.getRelationshipTypes()
                .then(result => {
                    this.relationships = result;
                    this.filterResults();
                    this.isLoading = false;
                    if (this.relationships.length > 0) {
                        this.selected = this.relationships[0];
                        this.selectedChange.emit(this.selected)
                    }
                });
        }
    }

    findRelationshipIndex(id: number) {
        var index: number = -1;
        for (var relationship of this.relationships) {
            index++;
            if (relationship.ID == id) return index;
        }
    }

    deleteRelationship(id: number) {
        this.relationshipsService.deleteRelationship(id)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                this.showDelete = false;
                if (result.type != 'error') {
                    this.selected = this.relationships.length > 0 ? this.relationships[0] : null;
                    this.relationships.splice(this.findRelationshipIndex(id), 1);
                }
            });
    }

    saveRelationship(event) {
        this.relationshipsService.saveRelationship(event.relationship)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                this.getRelationships();
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

    displayTypeName(type: string) {
        if (!type) return "";
        return type.replace("Type", "");
    }

    public downloadRel(relationship: RelationshipType) {
        this.relationshipsService.exportRelationshipTypeItems(relationship);
    }
}
