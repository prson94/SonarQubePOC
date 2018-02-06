import { Component, Input, Output, OnChanges, SimpleChange, EventEmitter, ViewChild} from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { MessagesService } from '../../../services/messages.service';
import { RelationshipsService } from '../../../services/relationships.service';
import { BaseComponent } from '../../shared/base.component';
import { FusionAttributeItemDetailsComponent } from '../fusion-attribute-item-details.component';
import { ObjectRelationship, PossibleTechnicalRelationship } from '../../../models/relationship.model';
import { D3SObjectHelpers } from '../../../static/d3s-object-helpers';

@Component({
    selector: 'd3s-relationship-technical-relations',
    providers: [RelationshipsService],
    template: `                   
                <div *ngIf="!showEditor && !addTechnicalRelationship">
                    <h4>Technical Relations for <em>{{objectName}}/{{relationship?.Name}}</em></h4>
                    <input #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">                                              
                    <p-dataTable #dt [globalFilter]="gb" scrollable="true" scrollWidth="100%" [rowsPerPageOptions]="defaultPagingOptions" [value]="relations" selectionMode="single" [rows]="defaultInitialItemsPerPage" paginator="true" pageLinks="3" [(selection)]="selected" (onRowDblclick)="selected=$event.data;openFusionItem();">                                                                                                  
                        <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>
                        <p-column field="Name" header="Name" sortable="true" [style]="{'width':'250px'}">
                             <ng-template let-item="rowData" pTemplate type="body">                                
                                <d3s-preview-tooltip [objectType]="item.Object" [objectId]="item.ObjectID" (click)="openFusionItem()">{{item.Name}}</d3s-preview-tooltip>
                            </ng-template> 
                        </p-column>                         
                        <p-column field="TypeName" header="Type" sortable="true" [style]="{'width':'250px'}"></p-column>            
                        <p-column [style]="{width:'40px'}">
                            <ng-template let-item="rowData" pTemplate type="body">
                                <div class="RowTools" (click)="selected=item;openFusionItem()">                                
                                    <i class="fa fa-info"></i>
                                </div>
                            </ng-template>
                        </p-column>  
                        <p-column  [style]="{width:'28px'}">
                                <ng-template let-item="rowData" pTemplate type="body">
                                    <div class="RowTools" *ngIf="hasEdit">                                
                                        <a style="cursor:pointer;" (click)="selected=item;showEditor=true;" title="Edit"><i class="fa fa-pencil"></i></a>                                                                           
                                    </div>
                                </ng-template>
                        </p-column>                   
                        <p-column  [style]="{width:'28px'}">
                                <ng-template let-item="rowData" pTemplate type="body">
                                    <div class="RowTools" *ngIf="hasDelete">                                                    
                                        <a style="cursor:pointer;" (click)="selected=item;deleteItem(item);" title="Remove"><i class="fa fa-trash-o"></i></a>                                    
                                    </div>
                                </ng-template>
                        </p-column>           
                    </p-dataTable>
                    <div style="margin:15px" *ngIf="selected && selected.Object == 'FusionAttribute'">
                        <d3s-fusion-attribute-item-details [fusionAttributeId]="selected.ObjectID" [name]="selected.Name"></d3s-fusion-attribute-item-details>
                    </div>
                    <div style="margin:15px" *ngIf="selected && selected.Object != 'FusionAttribute'">
                        <object-detail [objectID]="selected.ObjectID" [objectType]="selected.Object"></object-detail>
                    </div>
                </div>
                <div *ngIf="addTechnicalRelationship && !showEditor">
                    <header>Add A <em>{{objectName}}/{{relationship?.Name}}</em> Technical Relation</header>
                    <div *ngIf="possibleTechnicalIntersectTypes.length > 0" class="form-instructions">What type of object would you like to add a technical relationship to the relationship <b>{{relationship.Name}} / {{objectName}}</b>?</div>
                    <div class="row" *ngIf="possibleTechnicalIntersectTypes.length > 0">
                        <div class="col s12">                            
                            <div class="row">
                                <div class="col s12" *ngFor="let p of possibleTechnicalIntersectTypes"><a style="cursor:pointer" (click)="showEditor=true;selectedIntersectType=p.IntersectTypeID">{{getFriendlyName(p.ObjectType)}} - {{p.Title}}</a></div>
                            </div>
                        </div>
                        <div class="col s12">&nbsp;</div>
                        <div class="col s12">
                            <button pButton type="button" (click)="closeAddTech();" label="Cancel" style="width: 150px;"></button>
                        </div>
                    </div>    
                    <div class="row" *ngIf="possibleTechnicalIntersectTypes.length == 0">                
                        <div class="center">This relationship type does not have any technical relationship types configured.</div>
                        <div class="col s12">&nbsp;</div>
                        <div class="col s12">
                            <button pButton type="button" (click)="closeAddTech();" label="Cancel" style="width: 150px;"></button>
                        </div>
                    </div>
                </div>
                <d3s-dynamic-editor *ngIf="showRelEditor()"  [createUri]="'form/dynamicedit/create/intersect/'" [editUri]="'form/dynamicedit/edit/intersect/'" [objectID]="selectedIntersectType" [objectType]="'IntersectType'" [targetType]="'Intersect'" [targetTypeID]="relationship.ID" [title]="objectName + '/' + relationship?.Name + ' Technical Relationship'" [selection]="addTechnicalRelationship ? null : selected" [rowID]="'ID'" (saveClick)="saveTechRelationship($event)" (closeClick)="showEditor = false;"></d3s-dynamic-editor>
                <button *ngIf="!addTechnicalRelationship && !showEditor" pButton type="button" (click)="closeClick.emit();" label="Close" style="width: 150px;"></button>
                `
})

export class RelationshipTechnicalRelationsComponent extends BaseComponent implements OnChanges {
    @Input() relationship: any;
    @Input() objectName: string;

    @Output() closeClick = new EventEmitter();
    @Output() allTechnicalRelationshipsDeleted = new EventEmitter();

    @Input() addTechnicalRelationship: boolean;
    @Output() addTechnicalRelationshipChange = new EventEmitter();
    
    @ViewChild(FusionAttributeItemDetailsComponent) private fusionAttributeItemDetailsComponent: FusionAttributeItemDetailsComponent;

    @Input() hasEdit: boolean = true;
    @Input() hasDelete: boolean = true;

    private relations: any[] = [];
    private selected: any;
    private showEditor: boolean = false;

    private possibleTechnicalIntersectTypes: PossibleTechnicalRelationship[] = [];
    private selectedIntersectType: number;

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
        this.relationshipsService.getPossibleTechnicalRelations(this.relationship.ID).
            then(res => {
                this.possibleTechnicalIntersectTypes = res;
            });
    }

    private getFriendlyName(objectType): string {
        return D3SObjectHelpers.getObjectTypeFriendlyName(objectType);
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
        this.relationshipsService.deleteRelationshipItem(item.ID)
            .then(res => {
                this.relations = this.relations.filter(x => x.ID != item.ID);                
                if (this.relations.length == 0) this.allTechnicalRelationshipsDeleted.emit();
            });
        
    }

    private closeAddTech() {
        if (this.addTechnicalRelationship) {
            this.addTechnicalRelationship = false;
            this.addTechnicalRelationshipChange.emit(this.addTechnicalRelationship);
        }
    }

    private saveTechRelationship(event) {
        if (this.addTechnicalRelationship) {
            this.addTechnicalRelationship = false;
            this.addTechnicalRelationshipChange.emit(this.addTechnicalRelationship);
        }
        this.load();
        this.showEditor = false;
    }

    private showRelEditor() {
        return this.showEditor;
    }
}