import { Component, Input, Output, OnChanges, SimpleChange, EventEmitter, ViewChild} from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { MessagesService, RelationshipsService} from '../../services/index';
import { BaseComponent } from '../shared/base.component';
import { FusionAttributeItemDetailsComponent } from './fusion-attribute-item-details.component';

@Component({
    selector: 'd3s-relationship-technical-relations',
    providers: [RelationshipsService],
    template: `                   
                <div *ngIf="!showEditor">
                    <h4>Technical Relations for <em>{{objectName}}/{{relationship?.Name}}</em></h4>
                    <input #gb type="text" pInputText size="100" placeholder="Search..." style="margin-bottom:10px;width:100%;">                                              
                    <p-dataTable #dt [globalFilter]="gb"  scrollable="true" scrollWidth="100%" [rowsPerPageOptions]="[5,10,20]" [value]="relations" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" [(selection)]="selected" (onRowDblclick)="selected=$event.data;openFusionItem();">                                                                                                  
                        <p-column field="Name" header="Name" [sortable]="true" [style]="{'width':'250px'}"></p-column>                         
                        <p-column field="TypeName" header="Type" [sortable]="true" [style]="{'width':'250px'}"></p-column>            
                        <p-column [style]="{width:'40px'}">
                            <template let-item="rowData" pTemplate type="body">
                                <div class="RowTools" (click)="selected=item;openFusionItem()">                                
                                    <i class="fa fa-info"></i>
                                </div>
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
                    </p-dataTable>
                    <div style="margin:15px">
                        <d3s-fusion-attribute-item-details [fusionAttributeId]="selected?.ObjectID" [name]="selected?.Name"></d3s-fusion-attribute-item-details>
                    </div>
                </div>
                <d3s-dynamic-editor *ngIf="showEditor"  [createUri]="'form/dynamicedit/create/intersect/'" [editUri]="'form/dynamicedit/edit/intersect/'" [objectID]="selected?.IntersectTypeID" [objectType]="'IntersectType'" [targetType]="objectType" [targetTypeID]="objectID" [title]="'Fusion Relationship'" [selection]="addRelationship ? null : selected" [rowID]="'ID'" (saveClick)="saveTechRelationship($event)" (closeClick)="showEditor = false;"></d3s-dynamic-editor>
                <button *ngIf="!showEditor" pButton type="button" (click)="closeClick.emit();" label="Close" style="width: 150px;"></button>
                `
})

export class RelationshipTechnicalRelationsComponent extends BaseComponent implements OnChanges {
    @Input() relationship: any;
    @Input() objectName: string;

    @Output() closeClick = new EventEmitter();

    @ViewChild(FusionAttributeItemDetailsComponent) private fusionAttributeItemDetailsComponent: FusionAttributeItemDetailsComponent;

    private relations: any[] = [];
    private selected: any;
    private showEditor: boolean = false;

    constructor(private messagesService: MessagesService, protected router: Router, protected relationshipsService: RelationshipsService) {
        super();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.relationship) this.load();
    }
    
    private load() {
        this.isLoading = true;
        this.relationshipsService.getTechnicalRelationships('Intersect', this.relationship.ID).
            then(res => {
                this.relations = res;
                this.selected = (this.relations && this.relations.length > 0) ? this.relations[0] : null;
                this.isLoading = false;
            });
    }

    private openFusionItem() {
        if (!this.selected) return;

        if (!this.fusionAttributeItemDetailsComponent) {
            console.log("ERROR UNABLE TO FIND DETAILS COMPONENT");

            return;
        }
        
        this.fusionAttributeItemDetailsComponent.openItemInFusion();        
    }

    private deleteItem(item) {
        console.log(item.ID);
        this.relationshipsService.deleteRelationshipItem(item.ID)
            .then(res => {
                let indx = this.relations.findIndex(x => x.ID == item.ID);

                    if (indx >= 0) {
                        this.relations.splice(indx, 1);
                    }                
            });
        
    }

    private saveTechRelationship(event) {        
        this.load();
        this.showEditor = false;

    }
}


