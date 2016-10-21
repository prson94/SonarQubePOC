import { Input, Output, Component, OnChanges, SimpleChange, ViewChild } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { RelationshipsService } from '../../services/index';
import { ObjectRelationshipCount } from '../../models/relationship.model';
import { DynamicRelationshipGridComponent } from '../shared/dynamic-relationship-grid.component';
import { Permission } from '../../models/permission.model'

@Component({
    selector: 'd3s-object-relationships',
    providers: [RelationshipsService],    
    styles: [`
    div.relationship-container{
        max-height: 360px;min-height:200px;
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
                    <d3s-tile-actions [hasAdd]="hasRelationships && hasRelationshipCreatePermissions()" [hasExport]="enableExport()" (exportClick)="export()" (addClick)="showAddRelationship = true;" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
                </header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div *ngIf="!isLoading && hasRelationships" class="row" style="padding-left:10px;padding-bottom:5px;">
                    <label pTooltip="If you would like to see relationship types that have no relationships established click here.  In order to setup relations between types with no relations you need to enable this option also.">
                        <input type="checkbox" [(ngModel)]="showEmptyRelationshipTypes">Show relationship types with no relations established.
                    </label>
                </div>
                <div *ngIf="!isLoading && hasRelationships" class="row">
                    <div class="col l3 s12 relationship-container"><!--left nav-->
                        <template ngFor let-rel [ngForOf]="relationshipItems">                        
                            <div class="row relationship" *ngIf="(rel.Count > 0 && !showEmptyRelationshipTypes) || showEmptyRelationshipTypes" [ngClass]="{'active' : isSelected(rel)}" (click)="selected=rel;">
                                <div class="col s10 name"><i class="fa inactive-tool-icon" [ngClass]="{'fa-book':rel.Object=='ArtifactType','fa-sitemap':rel.Object=='TaxonomyType','fa-university':rel.Object=='PolicyType','fa-database':rel.Object=='FusionAttributeType','fa-pie-chart':rel.Object=='RuleType'}" [pTooltip]="rel.Object | technicalNameToDisplayValue"></i> {{rel.Name}}</div>
                                <div class="col s2 count center" [ngClass]="{'empty-count': rel.Count == 0, 'count': rel.Count != 0}">{{rel.Count}}</div>
                            </div>                        
                        </template>
                    </div>
                    <div class="col l9 s12">                        
                        <d3s-dynamic-relationship-grid [simpleFilter]="showSimpleFilter" [objectName]="objectName" [(addRelationship)]="showAddRelationship" (relationshipAdded)="addRelationship($event)" (relationshipRemoved)="removeRelationship()" [objectType]="objectType" [objectID]="objectID" [targetType]="selected?.Object" [targetTypeID]="selected?.ObjectID" [intersectTypeID]="selected?.IntersectTypeID" [hasEdit]="hasRelationshipUpdatePermissions()" [hasDelete]="hasRelationshipDeletePermissions()"></d3s-dynamic-relationship-grid>                        
                    </div>                    
                </div>
                <div class="row" *ngIf="!isLoading && !hasRelationships">
                        <div class="col s12">
                            <span class="center">No relationships types are currently setup for this item type.  Please contact your administrator or use the administration / metamodel / relationships module to configure them.</span>
                        </div>
                </div>
                `,
})

export class ObjectRelationshipsComponent extends BaseComponent implements OnChanges {
    @Input() objectType: string;
    @Input() objectID: number;
    @Input() objectName: string;
    @Input() objectPermissions: Permission[] = [];

    relationshipItems: ObjectRelationshipCount[] = [];
    selected: ObjectRelationshipCount;

    hasRelationships: boolean;
    showAddRelationship: boolean = false;
    showEmptyRelationshipTypes: boolean = false;
    
    @ViewChild(DynamicRelationshipGridComponent) private relGrid: DynamicRelationshipGridComponent;
    
    constructor(protected relationshipsService : RelationshipsService) {
        super();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        this.load();
    }

    load(): void {

        if (this.objectType == null || this.objectID == null)
            return;

        this.permissions = this.objectPermissions;

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
    

    export() {        
        if (!this.selected) return;
        this.relationshipsService.exportObjectRelationshipsToExcel(this.objectType, this.objectID, this.selected.Object, this.selected.ObjectID, this.selected.IntersectTypeID, false);
    }

    addRelationship(event) {
        if (!this.selected) return;
        this.selected.Count = this.selected.Count + event.count;
    }

    removeRelationship() {
        if (!this.selected) return;
        this.selected.Count--;
    }

    enableExport() {
        if (!this.selected) return false;
        return this.selected.Count > 0;
    }

    isSelected(item: ObjectRelationshipCount): boolean {        
        return (this.selected && this.selected == item);
    }

    relationshipsToShow() {        
        if (this.showEmptyRelationshipTypes)
            return this.relationshipItems;

        return this.relationshipItems.filter(x => x.Count > 0);
    }
}
