
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy, OnChanges, SimpleChange } from '@angular/core';
import { SelectItem  } from 'primeng/primeng';
import { ArtifactService, RelationshipsService, AttributeTypeService } from '../../services/index';
import { ArtifactType } from '../../models/artifact-type.model';
import { GridFilterExpression, GridFilterColumn, GridRelationshipFilterExpression, GridAttributeFilterExpression, GridFilterFieldType } from '../../models/grid-definition.model';
import { ObjectRelationship, RelatedItem } from '../../models/relationship.model';
import { AttributeType } from '../../models/attribute-type.model';

@Component({
    selector: 'd3s-artifact-column-filter',
    providers: [RelationshipsService, AttributeTypeService],
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
                    <div *ngFor="let filter of filters;let first=first;let last=last;let index=index" class="row filter">
                        <div class="col s1 FieldName">Field:</div>
                        <div class="col s4"><select [name]="'FilterField_' + index" required [(ngModel)]="filter.field" (change)="changeFilterField($event.target,filter)" style="width:100%;">                                            
                                                <option *ngFor="let p of fields" [value]="p.datafield">{{p.text}}</option></select>
                        </div>
                        <div class="col s4" [ngSwitch]="selectedFieldType(filter.field)">
                            <span *ngSwitchCase="'dropdownlist'">
                                <select [name]="'FilterValue_' + index" [(ngModel)]="filter.value" required style="width:100%;" placeholder="Choose a field">                                            
                                      <option *ngFor="let p of fieldFilters(filter.field)" [value]="p">{{p}}</option></select>
                            </span>
                            <input *ngSwitchDefault [name]="'FilterValue_' + index" type="text" required [(ngModel)]="filter.value" placeholder="Enter a value" style="width:100%;"> 
                        </div>
                        <div class="col s3">
                            <span (click)="addFilter()"><i *ngIf="last" class="fa fa-plus fa-2x" aria-hidden="true"></i></span> <span *ngIf="filters.length > 1" (click)="removeFilter(filter)"><i class="fa fa-minus fa-2x" aria-hidden="true"></i></span>
                        </div>                        
                    </div>
                    <div *ngIf="attributeFilter" class="row filter">
                        <div class="col s1 filter FieldName">
                            Attribute:
                        </div>
                        <div class="col s4 filter">
                            <div class="row">
                                <div class="col s12 FieldName">Attribute</div>
                                <div class="col s12"><select name="attributeName" style="width:100%;" placeholder="Choose an attribute" [(ngModel)]="attributeFilter.attributeType" (change)="attributeSelected($event.target?.value)">                                            
                                      <option></option>
                                      <option *ngFor="let p of attributeTypes" [value]="p.ID">{{p.Name}}</option></select>
                                </div>                                
                            </div>                       
                        </div>
                        <div class="col s4 filter">
                            <div class="row">
                                <div class="col s12 FieldName">Value</div>
                                <div class="col s12">
                                    <select name="attributeValue" style="width:100%;" placeholder="Choose a value" [(ngModel)]="attributeFilter.attributeSearchValue">                                            
                                      <option></option>
                                      <option *ngFor="let p of attributeValues" [value]="p">{{p}}</option></select>
                                </div>     
                            </div>
                        </div>
                        <div class="col s3"></div>
                    </div>
                    <div *ngIf="relationshipFilter" class="filter">
                        <div class="col s1 filter FieldName">
                            Relationship:                            
                        </div>
                        <div class="col s4 filter">
                            <div class="row">
                                <div class="col s12 FieldName">Type of relationship</div>
                                <div class="col s12"><select name="relationType" style="width:100%;" placeholder="Choose a type" [ngModel]="relationshipFilter.relationshipType?.IntersectTypeID" (ngModelChange)="relationshipSelected($event)">                                                                                  
                                      <option *ngFor="let p of relationshipTypes" [ngValue]="p.IntersectTypeID">{{p.TargetName}}</option></select>
                                </div>                                
                            </div>                       
                        </div>
                        <div class="col s4">
                            <div class="row">
                                <div class="col s12 FieldName">Relationship</div>
                                <div class="col s12">
                                    <p-multiSelect name="predicates" [options]="relationshipValues" [style]="{width:'100%'}" [(ngModel)]="relationshipFilter.objectIds"></p-multiSelect>
                                </div>     
                            </div>
                        </div>
                        <div class="col s3">
                            <div class="row">
                                <div class="col s12 FieldName">Connector</div>
                                <div class="col s12">
                                    <p-selectButton name="relationIncludeType" [options]="connectors" [(ngModel)]="relationshipFilter.includeType"></p-selectButton>
                                </div>     
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col s12 buttons">
                            <button pButton *ngIf="filters.length > 0 || relationshipFilter || attributeFilter" type="submit" [disabled]="!filterForm.form.valid" style="width: '150px';" label="Filter Results"></button>
                            <button pButton *ngIf="filters.length || relationshipFilter || attributeFilter" type="button" style="width: '150px';" label="Clear all Filters" (click)="clearFilter()"></button>
                            <button pButton *ngIf="!filters.length" type="button" style="width: '150px';" label="Add Filter" (click)="addFilter()"></button>
                            <button pButton *ngIf="!relationshipFilter && (relationshipTypes && relationshipTypes.length > 0)" type="button" style="width: '150px';" label="Add Relationship Filter" (click)="addRelationshipFilter()"></button>
                            <button pButton *ngIf="!attributeFilter && (attributeTypes && attributeTypes.length > 0)" type="button" style="width: '150px';" label="Add Attribute Filter" (click)="addAttributeFilter()"></button>
                        </div>
                    </div>
                </form>
                `    
})

export class ArtifactColumnFilterComponent implements OnInit, OnDestroy, OnChanges {
    @Input() fields: GridFilterColumn[];
    @Input() artifactType: ArtifactType;
    @Output() filterChanged = new EventEmitter();

    @Input() filters: GridFilterExpression[] = [];
    @Output() filtersChange = new EventEmitter();

    @Input() relationshipFilter: GridRelationshipFilterExpression = null;    
    @Output() relationshipFilterChange = new EventEmitter();

    @Input() attributeFilter: GridAttributeFilterExpression = null;
    @Output() attributeFilterChange = new EventEmitter();

    relationshipTypes: ObjectRelationship[];    
    relationshipValues: SelectItem[] = [];
    connectors: SelectItem[] = [{ label: "And", value: "All" }, { label: "Or", value: "Any" }];
        
    attributeTypes: AttributeType[];
    attributeValues: string[];
    
    constructor(private relationshipsService: RelationshipsService, private attributeTypeService: AttributeTypeService) {        
        
    }

    ngOnInit() {        

        if (this.attributeFilter && this.attributeFilter.attributeType)
            this.attributeSelected(this.attributeFilter.attributeType);        
    }

    ngOnDestroy() {
        
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.fields != null && this.fields.length > 0) {
            this.getAttributes();
            //fetch relationships for this artifacttypeid
            if (!this.relationshipTypes) this.getRelationshipTypes();
            if (this.relationshipFilter && this.relationshipFilter.relationshipType)
                this.loadRelationshipValues();
        }                
    }

    private onSubmit() {
        if (this.relationshipFilter) {            
            this.relationshipFilterChange.emit(this.relationshipFilter);        }

        if (this.attributeFilter) {
            this.attributeFilterChange.emit(this.attributeFilter);
        }

        this.filterChanged.emit({ filter: this.filters, relationships: this.relationshipFilter, attributes: this.attributeFilter });
    }

    public clearFilter() {
        this.filters.splice(0, this.filters.length);
        this.filtersChange.emit(this.filters);

        this.relationshipFilter = null;
        this.relationshipFilterChange.emit(this.relationshipFilter);

        this.attributeFilter = null;
        this.attributeFilterChange.emit(this.attributeFilter);
        
        this.filterChanged.emit({ filter: this.filters, relationshipFilter: this.relationshipFilter });
    }
       

    private selectedFieldType(field: string) {
        let res = this.fields.filter(f => f.datafield == field);
        if (res != null && res.length > 0) return res[0].columntype;
        return "";
    }

    private fieldFilters(field: string) {
        let res = this.fields.filter(f => f.datafield == field);
        if (res != null && res.length > 0) return res[0].filteritems;
        return undefined;
    }

    private changeFilterField(target, filter) {            
        if (this.selectedFieldType(target.value) == "dropdownlist")
            filter.condition = "EQUAL";
        else
            filter.condition = "CONTAINS";

        //determine the field type
        let res = this.fields.filter(f => f.datafield == target.value);
        if (res.length > 0) {
            if (res[0].hiddenfield)
                filter.fieldtype = GridFilterFieldType.Hidden;
            else if (res[0].relatedfield)
                filter.fieldtype = GridFilterFieldType.Relation;
            else
                filter.fieldtype = GridFilterFieldType.Normal;
        }        
    }

    private addFilter() {
        this.filters.push(new GridFilterExpression());
    }

    private getRelationshipTypes() {
        if (!this.artifactType || this.artifactType.ID <= 0) return;

        this.relationshipsService.getObjectRelations('ArtifactType', this.artifactType.ID)
            .then(result => {
                this.relationshipTypes = result;                
            });
    }

    private attributeSelected(target) {
        if (this.attributeFilter) console.log(this.attributeFilter.attributeSearchValue);
        this.attributeValues = [];
        this.attributeTypeService.getAttributeFilterValues('ArtifactType', this.artifactType.ID, target)
            .then(result => {
                this.attributeValues = result;
            });
    }

    private relationshipSelected(target) {
        if (!target) {
            console.log("ERROR RELATIONSELECTED TARGET IS NULL!");

            return;
        }

        var relTypes = this.relationshipTypes.filter(item => item.IntersectTypeID == target);

        if (relTypes.length < 1) {
            console.log("ERROR CANNOT FIND INRESECTTYPEID!", target);

            return;
        }

        //load values for this relationship
        this.relationshipFilter.relationshipType = relTypes[0];

        this.relationshipFilter.objectIds = [];

        this.loadRelationshipValues();
    }

    private loadRelationshipValues() {
        this.relationshipValues.splice(0, this.relationshipValues.length);

        this.relationshipsService.getRelatedObjects(this.relationshipFilter.relationshipType.TargetType, this.relationshipFilter.relationshipType.TargetTypeID).then(
            result => {
                for (let item of result) {
                    this.relationshipValues.push({ label: item.Name, value: item.ID });
                }
            });        
    }

    private addRelationshipFilter() {        
        this.relationshipFilter = new GridRelationshipFilterExpression();          
    }

    private addAttributeFilter() {
        this.attributeFilter = new GridAttributeFilterExpression();
    }

    private getAttributes() {
        if (!this.artifactType || this.artifactType.ID <= 0) return;

        this.attributeTypeService.getAttributeTypesForObject('ArtifactType', this.artifactType.ID)
            .then(result => {
                this.attributeTypes = result;
            });
    }

    private removeFilter(filter: GridFilterExpression) {        
        let index = this.filters.indexOf(filter);
        this.filters.splice(index, 1);        
    }

};