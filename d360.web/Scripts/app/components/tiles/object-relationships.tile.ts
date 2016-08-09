///<reference path="../../es6-shim.d.ts"/>
import { Input, Output, Component, OnChanges, SimpleChange } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { RelationshipsService } from '../../services/index';
import { TileActionsComponent } from '../tiles/tile-actions.component';
import { ObjectRelationshipCount } from '../../models/relationship.model';
import { ObjectRelationshipsByTypeTile } from './object-relationships-by-type.tile';

@Component({
    selector: 'd3s-object-relationships-tile',
    directives: [TileActionsComponent, ObjectRelationshipsByTypeTile],
    providers: [RelationshipsService],
    styles: [`
    div.relationship{
        padding:5px 3px 5px 0;
        cursor: pointer; cursor: hand;
    }
    div.relationship .name{
        text-transform: uppercase;
        color: rgba(84,164,218,1);
        font-weight:bold;
    }
    div.relationship .count{
        color:#ffffff;
        background-color: rgba(84,164,218,1);
        font-weight: bold;
        padding:2px;
        border-radius:3px;
    }
    div.active{
        background:#d3d5d8;
    }
  `],
    template: `
                <header>Relationships
                    <d3s-tile-actions [hasAdd]="true" [addTitle]="'Add Relationship'" (addClick)="add()"></d3s-tile-actions>                            
                </header>
                <div *ngIf="isLoading" style="width:100%; text-align:center;">
                    <div style="padding:10px;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                </div>
                <div *ngIf="!isLoading" class="row">
                    <div class="col s2"><!--left nav-->
                        <div class="row relationship" *ngFor="let rel of relationshipItems; let i = index" [ngClass]="{'active' : isSelected(rel)}" (click)="selected=rel;">
                            <div class="col s10 name">{{rel.Name}}</div>
                            <div class="col s2 count center">{{rel.Count}}</div>
                        </div>                        
                    </div>
                    <div class="col s10">
                        <!--Grid-->
                        <d3s-object-relationships-by-type-tile [objectType]="objectType" [objectID]="objectID" [targetType]="selected?.Object" [targetTypeID]="selected?.ObjectId"></d3s-object-relationships-by-type-tile>
                    </div>
                </div>
                `,
})

export class ObjectRelationshipsTile extends BaseComponent implements OnChanges {
    @Input() objectType: string;
    @Input() objectID: number;

    relationshipItems: ObjectRelationshipCount[] = [];
    selected: ObjectRelationshipCount;
    

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
                this.isLoading = false;
            });
    }

    add() {

    }

    isSelected(item: ObjectRelationshipCount): boolean {        
        return (this.selected && this.selected == item);
    }
}
