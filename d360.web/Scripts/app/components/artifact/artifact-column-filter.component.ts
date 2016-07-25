///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy, OnChanges, SimpleChange } from '@angular/core';
import { NgForm, REACTIVE_FORM_DIRECTIVES } from '@angular/forms';
import { Button, MultiSelect, SelectItem, SelectButton  } from 'primeng/primeng';
import { ArtifactService, RelationshipsService, AttributeTypeService } from '../../services/index';
import { ArtifactType } from '../../models/artifact-type.model';
import { GridFilterExpression, GridFilterColumn, GridRelationshipFilterExpression, GridAttributeFilterExpression, GridFilterFieldType } from '../../models/grid-definition.model';
import { ObjectRelationship, RelatedItem } from '../../models/relationship.model';
import { AttributeType } from '../../models/attribute-type.model';

@Component({
    selector: 'd3s-artifact-column-filter',
    directives: [Button, MultiSelect, SelectButton],
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
                                <div class="col s12"><select name="attributeName" style="width:100%;" placeholder="Choose an attribute" [(ngModel)]="attributeFilter.attributeType" (change)="attributeSelected($event.target)">                                            
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
                    <div *ngIf="relationshipFilters" class="filter">
                        <div class="col s1 filter FieldName">
                            Relationship:                            
                        </div>
                        <div class="col s4 filter">
                            <div class="row">
                                <div class="col s12 FieldName">Type of relationship</div>
                                <div class="col s12"><select name="relationType" style="width:100%;" placeholder="Choose a type" (change)="relationshipSelected($event.target)">                                            
                                      <option></option>
                                      <option *ngFor="let p of relationshipTypes" [value]="p.TargetType + '|' + p.TargetTypeID">{{p.TargetName}}</option></select>
                                </div>                                
                            </div>                       
                        </div>
                        <div class="col s4">
                            <div class="row">
                                <div class="col s12 FieldName">Relationship</div>
                                <div class="col s12">
                                    <p-multiSelect name="predicates" [options]="relationshipValues" [style]="{width:'100%'}" [(ngModel)]="relationItems"></p-multiSelect>
                                </div>     
                            </div>
                        </div>
                        <div class="col s3">
                            <div class="row">
                                <div class="col s12 FieldName">Connector</div>
                                <div class="col s12">
                                    <p-selectButton name="relationIncludeType" [options]="connectors" [(ngModel)]="relationshipFilters.includeType"></p-selectButton>
                                </div>     
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col s12 buttons">
                            <button pButton *ngIf="filters.length > 0 || relationshipFilters || attributeFilter" type="submit" [disabled]="!filterForm.form.valid" style="width: '150px';" label="Filter Results"></button>
                            <button pButton *ngIf="filters.length || relationshipFilters || attributeFilter" type="button" style="width: '150px';" label="Clear all Filters" (click)="clearFilter()"></button>
                            <button pButton *ngIf="!filters.length" type="button" style="width: '150px';" label="Add Filter" (click)="addFilter()"></button>
                            <button pButton *ngIf="!relationshipFilters && (relationshipTypes && relationshipTypes.length > 0)" type="button" style="width: '150px';" label="Add Relationship Filter" (click)="addRelationshipFilter()"></button>
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

    filters: GridFilterExpression[] = [];
    relationshipFilters: GridRelationshipFilterExpression = null;
    attributeFilter: GridAttributeFilterExpression = null;

    relationshipTypes: ObjectRelationship[];    
    relationshipValues: SelectItem[] = [];
    connectors: SelectItem[] = [{ label: "And", value: "All" }, { label: "Or", value: "Any" }];
    relationItems: string[];
    
    attributeTypes: AttributeType[];
    attributeValues: string[];
    
    constructor(private relationshipsService: RelationshipsService, private attributeTypeService: AttributeTypeService) {        
        
    }

    ngOnInit() {
        
    }

    ngOnDestroy() {
        
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.fields != null && this.fields.length > 0) {
            this.getAttributes();
            //fetch relationships for this artifacttypeid
            if (!this.relationshipTypes) this.getRelationshipTypes();
        }
    }

    private onSubmit() {
        if (this.relationItems && this.relationItems.length > 0 && this.relationshipFilters) {
            this.relationshipFilters.objectIds = this.relationItems.join(',');            
        }

        this.filterChanged.emit({ filter: this.filters, relationships: this.relationshipFilters, attributes: this.attributeFilter });
    }

    private clearFilter() {
        this.filters.splice(0, this.filters.length);
        this.relationshipFilters = null;
        this.attributeFilter = null;
        this.relationItems = [];
        this.filterChanged.emit({ filter: this.filters, relationshipFilter: this.relationshipFilters });
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
        this.attributeValues = [];
        this.attributeTypeService.getAttributeFilterValues('ArtifactType', this.artifactType.ID, target.value)
            .then(result => {
                this.attributeValues = result;
            });
    }

    private relationshipSelected(target) {
        //load values for this relationship
        this.relationItems = [];
        let objectInfo = target.value.split('|');
        if (objectInfo.length != 2) return;
        this.relationshipValues.splice(0, this.relationshipValues.length);
        this.relationshipFilters.objectType = objectInfo[0].replace("Type","");
        this.relationshipsService.getRelatedObjects(objectInfo[0], objectInfo[1]).then(
            result => {                
                for (let item of result) {
                    this.relationshipValues.push({ label: item.Name, value: item.ID });
                }   
            });        
    }

    private addRelationshipFilter() {        
        this.relationshipFilters = new GridRelationshipFilterExpression();      
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