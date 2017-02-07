import { Input, Component, EventEmitter, Output, OnInit, OnDestroy, OnChanges, SimpleChange } from '@angular/core';
import { SelectItem  } from 'primeng/primeng';
import { ArtifactService } from '../../services/artifacts.service';
import { RelationshipsService } from '../../services/relationships.service';
import { AttributeTypeService } from '../../services/attribute-type.service';
import { ArtifactTypeService } from '../../services/artifact-type.service';
import { ArtifactType } from '../../models/artifact-type.model';
import { GridFilterExpression, GridFilterColumn, GridRelationshipFilterExpression, GridAttributeFilterExpression, GridFilterFieldType, GridOwnerFilter } from '../../models/grid-definition.model';
import { ObjectRelationship, RelatedItem } from '../../models/relationship.model';
import { AttributeType } from '../../models/attribute-type.model';
import { FilterField, FilterFieldType, FilterExpression } from '../../models/filter-field.model';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-artifact-column-filter',
    providers: [RelationshipsService, AttributeTypeService, ArtifactTypeService],
    styles: [`
        div.filter {
            padding-bottom:5px;
        }    
        div.buttons {
            padding-left: 10px;
            padding-bottom: 5px;
        }    
  `],
    template: ` 
                <form (ngSubmit)="onSubmit()" #filterForm="ngForm">
                    <div *ngFor="let filter of internalFilters;let first=first;let last=last;let index=index" class="row filter">
                        <div class="col s1 FieldName">Filter:</div>
                        <div class="col s4"><select [name]="'FilterField_' + index" required [ngModel]="filter.Field" (ngModelChange)="filter.Field = $event;changeFilterField($event,filter)" style="width:100%;">
                                                        <option *ngFor="let p of availableFilters" [ngValue]="p">{{p.Name}}</option></select>
                        </div>
                        <div [ngSwitch]="filter.Type" class="col s4">
                            <span *ngSwitchCase="filterFieldType.Relationship">
                                <p-multiSelect required name="predicates" [options]="relationshipValues" [style]="{width:'100%'}" [ngModel]="filter?.Data?.objectIds" (ngModelChange)="filter.Data.objectIds = $event"></p-multiSelect>
                                <p-selectButton name="relationIncludeType" [options]="connectors" [ngModel]="filter?.Data?.includeType" (ngModelChange)="filter.Data.includeType = $event;"></p-selectButton>
                            </span>
                            <span *ngSwitchCase="filterFieldType.Attribute">
                                <select name="attributeValue" style="width:100%;" placeholder="Choose a value" [ngModel]="filter?.Data?.attributeSearchValue" (ngModelChange)="filter.Data.attributeSearchValue = $event">
                                      <option></option>
                                      <option *ngFor="let p of attributeValues" [value]="p">{{p}}</option>
                                </select>
                            </span>
                            <span *ngSwitchCase="filterFieldType.Owner">
                                <p-multiSelect name="owners" [options]="ownerValues" [style]="{width:'100%'}" [ngModel]="filter?.Data" (ngModelChange)="filter.Data = $event"></p-multiSelect>                                
                            </span>
                            <span *ngSwitchDefault>
                                <span  [ngSwitch]="filter.Field?.Data?.columntype">
                                    <span *ngSwitchCase="'dropdownlist'">
                                        <select [name]="'FilterValue_' + index" [ngModel]="filter?.Data?.value" (ngModelChange)="filter.Data.value = $event" required style="width:100%;" placeholder="Choose a field">                                            
                                            <option *ngFor="let p of filter.Field?.Data?.filteritems" [value]="p">{{p}}</option>
                                        </select>
                                    </span>
                                    <input *ngSwitchDefault [name]="'FilterValue_' + index" type="text" required [ngModel]="filter?.Data?.value" (ngModelChange)="filter.Data.value = $event" placeholder="Enter a value" style="width:100%;"> 
                                </span>                        
                            </span>
                        </div>
                        <div class="col s3">
                            <a (click)="addFilter()" class="fa-stack fa-lg overlayed-primary" pTooltip="Add Filter">                                
                                <i class="fa fa-filter fa-stack-1x"></i>
                                <i class="fa fa-plus fa-stack-1x overlayed-add"></i>                                
                            </a> 
                            <a *ngIf="internalFilters.length > 1" (click)="removeFilter(filter)" class="fa-stack fa-lg overlayed-primary" pTooltip="Remove Filter" >
                                <i class="fa fa-filter fa-stack-1x"></i>
                                <i class="fa fa-minus fa-stack-1x overlayed-remove"></i>                                
                            </a>
                        </div>                                                
                    </div>
                    <div class="row">
                        <div *ngIf="hasMultipleAttributes()" class="red-text center" style="font-weight:bold">**Warning: Only a single attribute filter is supported at a time!</div>
                        <div *ngIf="hasMultipleRelationships()" class="red-text center"  style="font-weight:bold">**Warning: Only a single relationship filter is supported at a time!</div>
                        <div *ngIf="hasMultipleOwners()" class="red-text center"  style="font-weight:bold">**Warning: Only a single owner filter is supported at a time!</div>
                        <div class="col s12 buttons">
                            <button pButton *ngIf="internalFilters.length > 0 || relationshipFilter || attributeFilter" type="submit" [disabled]="!filterForm.form.valid || hasMultipleAttributes() || hasMultipleRelationships() || hasMultipleOwners()" style="width: '150px';" label="Filter Results"></button>
                            <button pButton *ngIf="internalFilters.length || relationshipFilter || attributeFilter" type="button" style="width: '150px';" label="Clear all Filters" (click)="resetFilters()"></button>                        
                        </div>
                    </div>
                </form>
                `
})


export class ArtifactColumnFilterComponent implements OnInit, OnChanges {
    @Input() fields: GridFilterColumn[];
    @Input() artifactType: ArtifactType;
    @Output() filterChanged = new EventEmitter();

    @Input() filters: GridFilterExpression[] = [];
    @Output() filtersChange = new EventEmitter();

    @Input() relationshipFilter: GridRelationshipFilterExpression = null;    
    @Output() relationshipFilterChange = new EventEmitter();

    @Input() attributeFilter: GridAttributeFilterExpression = null;
    @Output() attributeFilterChange = new EventEmitter();

    @Input() ownerFilter: GridOwnerFilter = null;
    @Output() ownerFilterChange = new EventEmitter();

    relationshipTypes: ObjectRelationship[];    
    relationshipValues: SelectItem[] = [];
    
    connectors: SelectItem[] = [{ label: "And", value: "All" }, { label: "Or", value: "Any" }];
        
    attributeTypes: AttributeType[];
    attributeValues: string[];

    ownerValues: SelectItem[] = [];

    filterFieldType = FilterFieldType;

    private internalFilters: FilterExpression[] = [];

    private availableFilters: FilterField[] = [];

    private selectedFilter: any;

    private ownerShipFilter: FilterField = {
        Data: null,
        Name: 'Owned by',
        Type: FilterFieldType.Owner
    };

    constructor(private relationshipsService: RelationshipsService, private attributeTypeService: AttributeTypeService, private artifactTypeService: ArtifactTypeService) {        
        
    }

    ngOnInit() {        

        if (this.attributeFilter && this.attributeFilter.attributeType)
            this.attributeSelected(this.attributeFilter.attributeType);       

        if (this.ownerFilter && (this.ownerFilter.ownerGroups || this.ownerFilter.ownerUsers)) {            
            this.ownerSelected();
        }
        
    }
        
    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        var bHasInternalFilters = this.internalFilters.length > 0;
        if (changes["fields"] && this.fields != null && this.fields.length > 0) {            
            this.availableFilters = [];
            for (let field of this.fields) {                
                this.availableFilters.push({
                    Data: field, Name : `Field - ${field.text}`, Type : FilterFieldType.Field
                });
            }

            if (this.filters.length > 0 && !bHasInternalFilters) {
                this.internalFilters = this.internalFilters.filter(x => x.Type != FilterFieldType.Field);

                for (let filter of this.filters) {
                    this.internalFilters.push({
                        Type: FilterFieldType.Field,
                        Data: filter,
                        Field: this.availableFilters.filter(x => x.Type == FilterFieldType.Field && x.Data.datafield == filter.field)[0],
                    });
                }
            }
            else if (!bHasInternalFilters) {
                this.resetFilters();
            }
            

            this.getAttributes();
            //fetch relationships for this artifacttypeid
            if (!this.relationshipTypes) this.getRelationshipTypes();
            else this.addRelationshipTypesToAvailable(this.relationshipTypes)

            if (this.relationshipFilter && this.relationshipFilter.relationshipType && this.relationshipValues.length == 0)
                this.loadRelationshipValues(this.relationshipFilter.relationshipType);   

            this.availableFilters.push(this.ownerShipFilter);
        }
         
    }

    private onSubmit() {
        let hasAttributeFilter = false;
        let hasRelationFilter = false;
        let hasOwnerFilter = false;

        this.filters = [];
        this.ownerFilter = null;
        for (let internalFilter of this.internalFilters) {
            if (internalFilter.Type == FilterFieldType.Field) {
                this.filters.push(internalFilter.Data);
            }
            else if (internalFilter.Type == FilterFieldType.Attribute) {
                this.attributeFilter = internalFilter.Data;
                hasAttributeFilter = true;
            }
            else if (internalFilter.Type == FilterFieldType.Relationship) {                                
                this.relationshipFilter = internalFilter.Data;
                hasRelationFilter = true;                
            }
            else if (internalFilter.Type == FilterFieldType.Owner) {
                this.ownerFilter = new GridOwnerFilter();
                this.ownerFilter.ownerUsers = [];
                this.ownerFilter.ownerGroups = [];                
                for (let owner of internalFilter.Data) {
                    if (owner.Type.toUpperCase() == 'RESOURCE') {
                        this.ownerFilter.ownerUsers.push(owner.ID);
                    }
                    else {
                        this.ownerFilter.ownerGroups.push(owner.ID);
                    }
                }    
                hasOwnerFilter = true;                            
            }
        }

        if (!hasOwnerFilter && this.ownerFilter) this.ownerFilter = null;
        if (!hasRelationFilter && this.relationshipFilter) this.relationshipFilter = null;
        if (!hasAttributeFilter && this.attributeFilter) this.attributeFilter = null;

        this.attributeFilterChange.emit(this.attributeFilter);
        this.relationshipFilterChange.emit(this.relationshipFilter);
        this.ownerFilterChange.emit(this.ownerFilter);

        this.filtersChange.emit(this.filters);
                        
        this.filterChanged.emit({ filter: this.filters, relationships: this.relationshipFilter, attributes: this.attributeFilter });
    }

    public resetFilters() {
        this.internalFilters.splice(0, this.internalFilters.length);
        this.internalFilters.push(new FilterExpression());
        this.filters.splice(0, this.filters.length);
        this.filtersChange.emit(this.filters);

        this.relationshipFilter = null;
        this.relationshipFilterChange.emit(this.relationshipFilter);

        this.attributeFilter = null;
        this.attributeFilterChange.emit(this.attributeFilter);

        this.ownerFilter = null;
        this.ownerFilterChange.emit(this.ownerFilter);

        this.filterChanged.emit({ filter: this.filters, relationshipFilter: this.relationshipFilter });
    }
    

    private changeFilterField(target, filter) {             
        
        if (target.Type == FilterFieldType.Field) {
            filter.Data = new GridFilterExpression();
            filter.Data.field = target.Data.datafield;

            filter.Type = FilterFieldType.Field;

            if (target.Data.columntype == "dropdownlist")
                filter.Data.condition = "EQUAL";
            else
                filter.Data.condition = "CONTAINS";

            //determine the field type
            if (target.Data.hiddenfield)
                filter.Data.fieldtype = GridFilterFieldType.Hidden;
            else if (target.Data.relatedfield)
                filter.Data.fieldtype = GridFilterFieldType.Relation;
            else
                filter.Data.fieldtype = GridFilterFieldType.Normal;
        }
        else if (target.Type == FilterFieldType.Relationship) {
            filter.Data = new GridRelationshipFilterExpression();
            filter.Data.relationshipType = target.Data;

            filter.Type = FilterFieldType.Relationship;

            this.loadRelationshipValues(filter.Data.relationshipType);
        }
        else if (target.Type == FilterFieldType.Attribute) {
            filter.Data = new GridAttributeFilterExpression();
            filter.Data.attributeType = target.Data.ID;
            filter.Type = FilterFieldType.Attribute;

            this.attributeSelected(target.Data.ID);
        }
        else if (target.Type == FilterFieldType.Owner) {
            filter.Data = [];                         
            filter.Type = FilterFieldType.Owner;

            this.ownerSelected();
        }
    }

    private hasMultipleOwners() {
        return this.internalFilters.filter(x => x.Type == FilterFieldType.Owner).length > 1;
    }

    private hasMultipleRelationships() {
        return this.internalFilters.filter(x => x.Type == FilterFieldType.Relationship).length > 1;
    }

    private hasMultipleAttributes() {
        return this.internalFilters.filter(x => x.Type == FilterFieldType.Attribute).length > 1;
    }

    private addFilter() {
        this.internalFilters.push(new FilterExpression());
    }

    private getRelationshipTypes() {
        if (!this.artifactType || this.artifactType.ID <= 0) return;

        this.relationshipsService.getObjectRelations('ArtifactType', this.artifactType.ID)
            .then(result => {
                this.relationshipTypes = result;

                this.addRelationshipTypesToAvailable(this.relationshipTypes);                
            });
    }

    private addRelationshipTypesToAvailable(relTypes) { 
        for (let relationship of relTypes) {
            this.availableFilters.push({
                Data: relationship, Name: `Relationship - ${relationship.TargetName}${relationship.PredicateName ? '(' + relationship.PredicateName + ')': ''}`, Type: FilterFieldType.Relationship
            });
        }

        if (this.relationshipFilter) {            
            let indx = this.internalFilters.findIndex(x => x.Type == FilterFieldType.Relationship);

            if (indx >= 0 && indx < this.internalFilters.length)
                this.internalFilters.splice(indx, 1);

            this.internalFilters.push({
                Type: FilterFieldType.Relationship,
                Data: this.relationshipFilter,
                Field: this.availableFilters.filter(x => x.Type == FilterFieldType.Relationship &&  x.Data.IntersectTypeID == this.relationshipFilter.relationshipType.IntersectTypeID)[0],
            });            
        }        
    }

    private ownerSelected() {
        if (this.ownerValues.length > 0) return; //already loaded owners
        this.artifactTypeService.getPossibleArtifactOwners(this.artifactType.ID)
            .then(result => {
                
                for (let item of result) {
                    this.ownerValues.push({ label: item.Name, value: item });
                }

                //add an internal filter in case we need to init the ui this way
                if (this.ownerFilter) {                                        
                    var owners = [];                    
                    for (let group of this.ownerFilter.ownerGroups) {
                        //find a group in results with type group and id matching
                        let indx = result.findIndex(x => x.ID == group && x.Type == "Group");
                        if (indx >= 0 && indx < result.length) {
                            owners.push(result[indx]);                            
                        }
                    }

                    for (let user of this.ownerFilter.ownerUsers) {
                        let indx = result.findIndex(x => x.ID == user && x.Type == "Resource");
                        if (indx >= 0 && indx < result.length) {
                            owners.push(result[indx]);
                        }
                    }
                    var filter = new FilterExpression();
                    filter.Type = FilterFieldType.Owner;
                    filter.Data = owners;
                    filter.Field = this.ownerShipFilter;
                    
                    this.internalFilters.push(filter);                    
                }

            });
    }

    private attributeSelected(target) {        
        this.attributeValues = [];
        this.attributeTypeService.getAttributeFilterValues('ArtifactType', this.artifactType.ID, target)
            .then(result => {
                this.attributeValues = result;
            });
    }
        
    private loadRelationshipValues(relationshipType: ObjectRelationship) {
        this.relationshipValues.splice(0, this.relationshipValues.length);

        this.relationshipsService.getRelatedObjects(relationshipType.TargetType, relationshipType.TargetTypeID).then(
            result => {
                for (let item of result) {
                    this.relationshipValues.push({ label: item.Name, value: item.ID });
                }
            });        
    }

    private addRelationshipFilter() {        
        this.relationshipFilter = new GridRelationshipFilterExpression();          
    }
    
    private getAttributes() {
        if (!this.artifactType || this.artifactType.ID <= 0) return;

        this.attributeTypeService.getAttributeTypesForObject('ArtifactType', this.artifactType.ID)
            .then(result => {
                this.attributeTypes = result;

                for (let attributeType of this.attributeTypes) {
                    this.availableFilters.push({
                        Data: attributeType, Name: `Attribute - ${attributeType.Name}`, Type: FilterFieldType.Attribute
                    });
                }

                if (this.attributeFilter) {
                    this.internalFilters = this.internalFilters.filter(x => x.Type != FilterFieldType.Attribute);                    
                    
                    this.internalFilters.push({
                        Type: FilterFieldType.Attribute,
                        Data: this.attributeFilter,
                        Field: this.availableFilters.filter(x => x.Type == FilterFieldType.Attribute && x.Data.ID == this.attributeFilter.attributeType)[0],
                    });                    
                }
            });
    }

    private removeFilter(filter: FilterExpression) {        
        let index = this.internalFilters.indexOf(filter);
        this.internalFilters.splice(index, 1);                
    }
};

