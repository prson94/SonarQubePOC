import { Input, Component, EventEmitter, Output, OnInit, OnDestroy, OnChanges, SimpleChange, ChangeDetectorRef } from '@angular/core';
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
                        <div class="col s4">
                            <select [name]="'FilterField_' + index" required [(ngModel)]="filter.Field" (ngModelChange)="filter.Field = $event;changeFilterField($event,filter)" style="width:100%;">
                               <option  [value]=""></option>
                                <option *ngFor="let p of availableFilters" [ngValue]="p">{{p.Name}}</option>
                            </select>
                        </div>
                        <div [ngSwitch]="filter.Type" class="col s4">
                            <span *ngSwitchCase="filterFieldType.Relationship">
                                <p-multiSelect required [name]="'predicates_' + index" [options]="filter?.Data?.options" [style]="{width:'100%'}" [ngModel]="filter?.Data?.objectIds" (ngModelChange)="filter.Data.objectIds = $event"></p-multiSelect>
                                <p-selectButton [name]="'relationIncludeType_' + index" [options]="connectors" [ngModel]="filter?.Data?.includeType" (ngModelChange)="filter.Data.includeType = $event;"></p-selectButton>
                            </span>
                            <span *ngSwitchCase="filterFieldType.Attribute">
                                <select [name]="'attributeValue_' + index" style="width:100%;" placeholder="Choose a value" [ngModel]="filter?.Data?.attributeSearchValue" (ngModelChange)="filter.Data.attributeSearchValue = $event">
                                      <option></option>
                                      <option *ngFor="let p of attributeValues" [value]="p">{{p}}</option>
                                </select>
                            </span>
                            <span *ngSwitchCase="filterFieldType.Owner">
                                <p-multiSelect name="owners" [options]="ownerValues" [style]="{width:'100%'}" [ngModel]="filter?.Data" (ngModelChange)="filter.Data = $event"></p-multiSelect>                                
                            </span>
                            <span *ngSwitchDefault>
                                <span  [ngSwitch]="filter.Field?.Data?.filtertype">
                                    <span *ngSwitchCase="'list' || 'checkedlist'"   >
                                        <d3s-loading [isLoading]="isLoadingFilter"></d3s-loading>
                                        <select *ngIf="!isLoadingFilter" [name]="'FilterValue_' + index" [ngModel]="filter?.Data?.value" (ngModelChange)="filter.Data.value = $event" required style="width:100%;" placeholder="Choose a field">                                            
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
                        <div *ngIf="hasMultipleOwners()" class="red-text center"  style="font-weight:bold">**Warning: Only a single owner filter is supported at a time!</div>
                        <div class="col s12 buttons">
                            <button pButton *ngIf="internalFilters.length > 0" type="submit" [disabled]="!filterForm.form.valid || hasMultipleOwners()" style="width: '150px';" label="Filter Results"></button>
                            <button pButton *ngIf="internalFilters.length" type="button" style="width: '150px';" label="Clear all Filters" (click)="resetFilters()"></button>                        
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

    @Input() relationshipFilters: GridRelationshipFilterExpression[] = [];    
    @Output() relationshipFiltersChange = new EventEmitter();

    @Input() attributeFilters: GridAttributeFilterExpression[] = [];
    @Output() attributeFiltersChange = new EventEmitter();

    @Input() ownerFilter: GridOwnerFilter = null;
    @Output() ownerFilterChange = new EventEmitter();

    relationshipTypes: ObjectRelationship[];    
    connectors: SelectItem[] = [{ label: "And", value: "All" }, { label: "Or", value: "Any" }];
    attributeTypes: AttributeType[];
    attributeValues: string[];

    ownerValues: SelectItem[] = [];

    filterFieldType = FilterFieldType;

    private internalFilters: FilterExpression[] = [];

    private availableFilters: FilterField[] = [];

    private selectedFilter: any;

    private isLoadingFilter = false;

    private ownerShipFilter: FilterField = {
        Data: null,
        Name: 'Owned by',
        Type: FilterFieldType.Owner
    };

    constructor(private relationshipsService: RelationshipsService, private attributeTypeService: AttributeTypeService, private artifactTypeService: ArtifactTypeService, private ref: ChangeDetectorRef) {        
        
    }

    ngOnInit() {        
        if (this.attributeFilters.length > 0)
            this.attributeSelected(this.attributeFilters[0].attributeType);

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
                    Data: field, Name : `${field.text}`, Type : FilterFieldType.Field
                });
            }

            if (this.filters.length > 0 && !bHasInternalFilters) {
                this.internalFilters = this.internalFilters.filter(x => x.Type != FilterFieldType.Field);

                for (let filter of this.filters) {
                    let f = {
                        Type: FilterFieldType.Field,
                        Data: filter,
                        Field: this.availableFilters.filter(x => x.Type == FilterFieldType.Field && x.Data.datafield == filter.field)[0],
                    };
                    if (f.Field)
                        this.changeFilterField(f.Field, f);
                    this.internalFilters.push(f);
                }
            }
            else if (this.relationshipFilters.length > 0 && !bHasInternalFilters) {
                // dont clear the relationship filters
            }
            else if (!bHasInternalFilters) {                
                this.resetFilters();
            }

            this.getAttributes();
            this.getRelationships();
            this.availableFilters.push(this.ownerShipFilter);
        }
    }

    private onSubmit() {
        let hasOwnerFilter = false;

        this.filters = [];
        this.relationshipFilters = [];
        this.attributeFilters = [];
        this.ownerFilter = null;
        for (let internalFilter of this.internalFilters) {

            if (internalFilter.Type == FilterFieldType.Field) {
                this.filters.push(internalFilter.Data);
            }
            else if (internalFilter.Type == FilterFieldType.Attribute) {
                this.attributeFilters.push(internalFilter.Data);
            }
            else if (internalFilter.Type == FilterFieldType.Relationship) {                                
                this.relationshipFilters.push(internalFilter.Data);
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
        
        this.attributeFiltersChange.emit(this.attributeFilters);
        this.relationshipFiltersChange.emit(this.relationshipFilters);
        this.ownerFilterChange.emit(this.ownerFilter);

        this.filtersChange.emit(this.filters);
                        
        this.filterChanged.emit();
    }

    public resetFilters() {
        this.internalFilters.splice(0, this.internalFilters.length);
        this.internalFilters.push(new FilterExpression());
        this.filters.splice(0, this.filters.length);
        this.filtersChange.emit(this.filters);

        this.relationshipFilters.splice(0, this.relationshipFilters.length);
        this.relationshipFiltersChange.emit(this.relationshipFilters);

        this.attributeFilters.splice(0, this.attributeFilters.length);
        this.attributeFiltersChange.emit(this.attributeFilters);

        this.ownerFilter = null;
        this.ownerFilterChange.emit(this.ownerFilter);

        this.filterChanged.emit({ filter: this.filters, relationshipFilter: this.relationshipFilters });
    }

    private changeFilterField(target, filter) {    

        if (target.Type == FilterFieldType.Field) {
            if (!filter.Data)
                filter.Data = new GridFilterExpression();
            filter.Data.field = target.Data.datafield;
            filter.Type = FilterFieldType.Field;
            
            if (target.Data.filtertype == 'list' && target.Data.datafield != null && target.Data.datafield.toLowerCase() != 'parent') {
                let fieldId: number = +target.Data.datafield.replace('Field', '');
                if (!isNaN(fieldId)) {
                    this.isLoadingFilter = true;
                    this.artifactTypeService.getFilterListItems(this.artifactType.ID, 'ArtifactType', fieldId).
                        then(r => {
                            filter.Field.Data.filteritems = r;
                            this.isLoadingFilter = false;
                            this.ref.markForCheck();
                        });
                }
            }
            else if (target.Data.filtertype == 'list' && target.Data.datafield != null && target.Data.datafield.toLowerCase() == 'parent') {
                this.isLoadingFilter = true;
                this.artifactTypeService.getObjectTypeParentsListItems(this.artifactType.ID, 'ArtifactType').
                    then(r => {
                        filter.Field.Data.filteritems = r;
                        this.isLoadingFilter = false;
                        this.ref.markForCheck();
                    });
            }

            if (target.Data.columntype == "dropdownlist" || target.Data.columntype == "numberinput")
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

            this.loadRelationshipValues(filter.Data);
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

    private addFilter() {
        this.internalFilters.push(new FilterExpression());
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

    //#region Attribute Logic

    private attributeSelected(target) {        
        this.attributeValues = [];
        this.attributeTypeService.getAttributeFilterValues('ArtifactType', this.artifactType.ID, target)
            .then(result => {
                this.attributeValues = result;
            });
    }
   
    private getAttributes() {
        try {
            if (!this.artifactType || this.artifactType.ID <= 0) return;

            this.attributeTypeService.getAttributeTypesForObject('ArtifactType', this.artifactType.ID)
                .then(result => {
                    this.attributeTypes = result;

                    for (let attributeType of this.attributeTypes) {
                        this.availableFilters.push({
                            Data: attributeType, Name: `${attributeType.Name}`, Type: FilterFieldType.Attribute
                        });
                    }

                    if (this.attributeFilters) {
                        this.internalFilters = this.internalFilters.filter(x => x.Type != FilterFieldType.Attribute);

                        for (let att of this.attributeFilters) {
                            this.internalFilters.push({
                                Type: FilterFieldType.Attribute,
                                Data: att,
                                Field: this.availableFilters.filter(x => x.Type == FilterFieldType.Attribute && x.Data.ID == att.attributeType)[0],
                            });
                        }                    
                    }
                });
        }
        catch (e) {
            console.log("Error: " + e);
        }
    }

    //#endregion

    //#region Relationship Logic

    private loadRelationshipValues(expr: GridRelationshipFilterExpression) {
        return this.relationshipsService
            .getRelatedObjects(expr.relationshipType.TargetType, expr.relationshipType.TargetTypeID, expr.relationshipType.IntersectTypeID)
            .then(result => {
                expr.options = [];
                for (let item of result) {
                    expr.options.push({ label: item.Name, value: item.ID });
                }
                this.ref.markForCheck();
            });
    }

    private addRelationshipTypesToAvailable(relTypes) {
        for (let relationship of relTypes) {
            this.availableFilters.push({
                Data: relationship, Name: `${relationship.TargetName}${relationship.PredicateName ? '(' + relationship.PredicateName + ')' : ''}`, Type: FilterFieldType.Relationship
            });
        }

        if (this.relationshipFilters) {
            this.internalFilters = this.internalFilters.filter(x => x.Type != FilterFieldType.Relationship);

            for (let rel of this.relationshipFilters) {
                this.loadRelationshipValues(rel).then(r => {
                    this.internalFilters.push({
                        Type: FilterFieldType.Relationship,
                        Data: rel,
                        Field: this.availableFilters.filter(x => x.Type == FilterFieldType.Relationship && x.Data.IntersectTypeID == rel.relationshipType.IntersectTypeID)[0],
                    });
                });
            }
        }
    }

    private getRelationships() {
        try {
            //fetch relationships for this artifacttypeid
            if (!this.relationshipTypes) {
                if (!this.artifactType || this.artifactType.ID <= 0) return;

                this.relationshipsService
                    .getObjectRelations('ArtifactType', this.artifactType.ID)
                    .then(result => {
                        this.relationshipTypes = result;
                        this.addRelationshipTypesToAvailable(this.relationshipTypes);  
                        this.ref.markForCheck();
                    });
            }
            else {
                this.addRelationshipTypesToAvailable(this.relationshipTypes);
                this.ref.markForCheck();
            }
        }
        catch (e) {
            console.log("Error: " + e);
        }
    }

    //#endregion

    private removeFilter(filter: FilterExpression) {        
        let index = this.internalFilters.indexOf(filter);
        this.internalFilters.splice(index, 1);
    }
};

