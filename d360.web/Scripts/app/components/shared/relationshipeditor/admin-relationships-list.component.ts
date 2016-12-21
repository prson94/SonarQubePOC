import { Input, Component, Output, EventEmitter, OnChanges, SimpleChange } from '@angular/core';
import { RelationshipsService } from '../../../services/relationships.service';
import { MessagesService  } from '../../../services/messages.service';
import { Relationship } from '../../../models/relationship.model';
import { BaseComponent } from '../../shared/base.component';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-admin-relationships-list',
    providers: [RelationshipsService],    
    template: `
                <header *ngIf="!showEditor && !showDelete">Relationship Types
                    <d3s-tile-actions [hasAdd]="true" (addClick)="add()" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
                </header>    
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div  *ngIf="!showEditor && !showDelete && !isLoading" class="row">                    
                    <div class="col s12">
                        <input [hidden]="!showSimpleFilter" #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">                    
                        <p-dataTable #dt [globalFilter]="gb" [value]="relationships" selectionMode="single" [rows]="20" [paginator]="true" [pageLinks]="3" expandableRows="true" [selection]="selected" (selectionChange)="selected=$event;selectedChange.emit(selected)" (onRowDblclick)="selected=$event.data;selectedChange.emit(selected);showEditor=true;" >                            
                            <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                            <p-column field="SubjectName" header="Subject" sortable="custom" (sortFunction)="columnSort($event)" [filter]="!showSimpleFilter">
                                <template let-col let-item="rowData" pTemplate type="body">
                                    <span>{{item?.SubjectName}}<span style="color: #999;font-size:75%;"> ({{displayTypeName(item?.Subject)}})</span></span>
                                </template>
                            </p-column>
                            <p-column field="PredicateName" header="Predicate" sortable="custom" (sortFunction)="columnSort($event)" [filter]="!showSimpleFilter"></p-column>                                
                            <p-column field="ObjectName" header="Side 2 Name" sortable="custom" (sortFunction)="columnSort($event)" [filter]="!showSimpleFilter">
                                <template let-col let-item="rowData" pTemplate type="body">
                                    <span>{{item?.ObjectName}}<span style="color: #999;font-size:75%;"> ({{displayTypeName(item?.Object)}})</span></span>
                                </template>
                            </p-column>
                            <p-column [style]="{width:'40px'}">
                                <template let-relationship="rowData" pTemplate type="body">
                                    <div class="RowTools">
                                        <a style="cursor:pointer;" (click)="selected=relationship;showEditor=true"><i class="fa fa-pencil"></i></a>                                        
                                    </div>
                                </template>
                            </p-column>                            
                            <p-column  [style]="{width:'40px'}">
                                <template let-relationship="rowData" pTemplate type="body">
                                    <div class="RowTools">                                
                                        <a style="cursor:pointer;" (click)="selected=relationship;showDelete=true"><i class="fa fa-trash-o"></i></a>                                    
                                    </div>
                               </template>
                           </p-column>    
                        </p-dataTable>  
                    </div>
                </div>
                <d3s-delete-form *ngIf="showDelete"
                    [callback]="theDeleteCallback"
                    [itemId]="selected?.ID"
                    [method]="'callback'"
                    [prompt]="'Are you sure you want to delete the relationship [' + [selected?.SubjectName] + ' / ' + [selected?.ObjectName]  + ']?'"                                         
                    (onCancel)="showDelete=false;"
                ></d3s-delete-form>  
                <d3s-admin-relationships-editor *ngIf="showEditor" [relationshipID]="selected?.ID" (saveClick)="saveRelationship($event)" (closeClick)="closeEditor()"></d3s-admin-relationships-editor>       
            `    
})

export class AdminRelationshipsListComponent extends BaseComponent implements OnChanges {
    relationships: Relationship[] = [];
    
    @Input() filterToName: string;

    @Input() selected: Relationship;
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

    private filterResults() {
        if (this.filterToName && this.filterToName.length > 0) {
            var search = this.filterToName.toLowerCase();            
            this.relationships = this.relationships.filter(item => item.Object && item.Object.toLowerCase().includes(search) || item.Subject && item.Subject.toLowerCase().includes(search) || item.ObjectName && item.ObjectName.toLowerCase().includes(search) || item.SubjectName && item.SubjectName.toLowerCase().includes(search));
        }
    }

    getRelationships() {
        this.isLoading = true;
        this.relationshipsService.getRelations()
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

    displayTypeName(type: string) {
        if (!type) return "";
        return type.replace("Type", "");
    }

    private columnSort(event) {
        //event.field = Field to sort
        //event.order = Sort order, 1 ascending , -1 descending                        
        this.relationships = _.orderBy(this.relationships, [item => item[event.field] ? item[event.field].toLowerCase() : item[event.field]], [event.order == -1 ? 'desc' : 'asc']);
    }
}
