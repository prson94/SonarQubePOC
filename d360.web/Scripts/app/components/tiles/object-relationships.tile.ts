///<reference path="../../es6-shim.d.ts"/>
import { Input, Output, Component, OnChanges, SimpleChange } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { RelationshipsService } from '../../services/index';
import { TileActionsComponent } from '../tiles/tile-actions.component';
import { ObjectRelationshipCount } from '../../models/relationship.model';
import { DynamicRelationshipGridComponent } from '../shared/dynamic-relationship-grid.component';

@Component({
    selector: 'd3s-object-relationships-tile',
    directives: [TileActionsComponent, DynamicRelationshipGridComponent],
    providers: [RelationshipsService],
    styles: [`
    div.relationship-container{
        max-height: 400px;min-height:200px;
        overflow: auto;
    }
    div.relationship{
        padding:5px 3px 5px 0;
        cursor: pointer; cursor: hand;        
    }
    div.relationship .name{
        text-transform: uppercase;
        color: rgba(84,164,218,1);
        font-weight:bold;
    }
    div.relationship .count, div.relationship .empty-count{
        color:#ffffff;        
        font-weight: bold;
        padding:2px;
        border-radius:3px;
    }
    div.active{
        background:#d3d5d8;
    }
    div.relationship .count{
        background-color: rgba(84,164,218,1);
    }
    div.relationship .empty-count{
        background-color:#646464;
    }

  `],
    template: `
                <header>Relationships
                    <d3s-tile-actions [hasAdd]="true" [hasExport]="true" [addTitle]="'Add Relationship'" (addClick)="add()"></d3s-tile-actions>                            
                </header>
                <div *ngIf="isLoading" style="width:100%; text-align:center;">
                    <div style="padding:10px;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                </div>
                <div *ngIf="!isLoading && hasRelationships" class="row">
                    <div class="col l3 s12 relationship-container"><!--left nav-->
                        <div class="row relationship" *ngFor="let rel of relationshipItems; let i = index" [ngClass]="{'active' : isSelected(rel)}" (click)="selected=rel;">
                            <div class="col s10 name">{{rel.Name}}</div>
                            <div class="col s2 count center" [ngClass]="{'empty-count': rel.Count == 0, 'count': rel.Count > 0}">{{rel.Count}}</div>
                        </div>                        
                    </div>
                    <div class="col l9 s12">                        
                        <d3s-dynamic-relationship-grid [(addRelationship)]="showAddRelationship" (relationshipAdded)="addRelationship()" (relationshipRemoved)="removeRelationship()" [objectType]="objectType" [objectID]="objectID" [targetType]="selected?.Object" [targetTypeID]="selected?.ObjectID" [intersectTypeID]="selected?.IntersectTypeID"></d3s-dynamic-relationship-grid>                        
                    </div>                    
                </div>
                <div class="row" *ngIf="!isLoading && !hasRelationships">
                        <div class="col s12">
                            <span class="center">No relationships types are currently setup for this item type.  Please contact your administrator or use the administration / metamodel / relationships module to configure them.</span>
                        </div>
                </div>
                `,
})

export class ObjectRelationshipsTile extends BaseComponent implements OnChanges {
    @Input() objectType: string;
    @Input() objectID: number;

    relationshipItems: ObjectRelationshipCount[] = [];
    selected: ObjectRelationshipCount;

    hasRelationships: boolean;
    showAddRelationship: boolean = false;
    
    constructor(protected relationshipsService : RelationshipsService) {
        super();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        this.load();
    }

    load(): void {

        if (this.objectType == null || this.objectID == null)
            return;

        this.isLoading = true;

        this.loadRelationshipItems();
    }

    loadRelationshipItems() {
        this.relationshipsService.getRelationshipCounts(this.objectType, this.objectID)
            .then(result => {
                this.relationshipItems = result;
                this.selected = this.relationshipItems.length > 0 ? this.relationshipItems[0] : null;
                this.hasRelationships = (this.relationshipItems && this.relationshipItems.length > 0);
                
                this.isLoading = false;
            });
    }

    add() {
        this.showAddRelationship = true;
    }

    addRelationship() {
        if (!this.selected) return;
        this.selected.Count++;
    }

    removeRelationship() {
        if (!this.selected) return;
        this.selected.Count--;
    }

    isSelected(item: ObjectRelationshipCount): boolean {        
        return (this.selected && this.selected == item);
    }
}
