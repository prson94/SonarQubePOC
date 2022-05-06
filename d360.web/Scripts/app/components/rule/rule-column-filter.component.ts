import { Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChange } from '@angular/core';
import { SelectItem } from 'primeng/api';
import { RelationshipsService } from '../../services/relationships.service';
import {
    GridFilterColumn,
    GridFilterExpression,
    GridFilterFieldType,
    GridRelationshipFilterExpression
} from '../../models/grid-definition.model';
import { ObjectRelationship } from '../../models/relationship.model';
import { FilterExpression, FilterField, FilterFieldType } from '../../models/filter-field.model';

@Component({
    selector: 'd3s-rule-column-filter',
    providers: [RelationshipsService],
    styles: [`
        div.filter {
            padding-bottom: 5px;
        }

        div.buttons {
            padding-left: 10px;
            padding-bottom: 5px;
        }
    `],
    templateUrl: './rule-column-filter.component.html'
})

export class RuleColumnFilterComponent implements OnInit, OnChanges {
    @Input() fields: GridFilterColumn[];
    @Output() filterChanged = new EventEmitter();

    @Input() filters: GridFilterExpression[] = [];
    @Output() filtersChange = new EventEmitter();

    @Input() relationshipFilter: GridRelationshipFilterExpression = null;
    @Output() relationshipFilterChange = new EventEmitter();

    relationshipTypes: ObjectRelationship[];
    relationshipValues: SelectItem[] = [];
    connectors: SelectItem[] = [{ label: "And", value: "All" }, { label: "Or", value: "Any" }];
    filterFieldType = FilterFieldType;

    internalFilters: FilterExpression[] = [];
    availableFilters: FilterField[] = [];
    selectedFilter: any;

    constructor(
        private relationshipsService: RelationshipsService
    ) {
    }

    ngOnInit() {

    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        var bHasInternalFilters = this.internalFilters.length > 0;

        if (changes["fields"] && this.fields != null && this.fields.length > 0) {
            this.availableFilters = [];

            for (let field of this.fields) {
                this.availableFilters.push({
                    Data: field, Name: $localize`Field` + ` - ${field.text}`, Type: FilterFieldType.Field
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

            //fetch relationships for this artifacttypeid
            if (!this.relationshipTypes) {
                this.getRelationshipTypes();
            } else {
                this.addRelationshipTypesToAvailable(this.relationshipTypes);
            }

            if (this.relationshipFilter && this.relationshipFilter.relationshipType && this.relationshipValues.length == 0) {
                this.loadRelationshipValues(this.relationshipFilter.relationshipType);
            }
        }
    }

    onSubmit() {
        this.filters = [];

        for (let internalFilter of this.internalFilters) {
            if (internalFilter.Type == FilterFieldType.Field) {
                this.filters.push(internalFilter.Data);
            } else if (internalFilter.Type == FilterFieldType.Relationship) {
                this.relationshipFilter = internalFilter.Data;
            }
        }

        if (this.relationshipFilter) {
            this.relationshipFilterChange.emit(this.relationshipFilter);
        }

        this.filtersChange.emit(this.filters);

        this.filterChanged.emit({
            filter: this.filters,
            relationships: this.relationshipFilter
        });
    }

    public resetFilters() {
        this.internalFilters.splice(0, this.internalFilters.length);
        this.internalFilters.push(new FilterExpression());
        this.filters.splice(0, this.filters.length);
        this.filtersChange.emit(this.filters);

        this.relationshipFilter = null;
        this.relationshipFilterChange.emit(this.relationshipFilter);


        this.filterChanged.emit({ filter: this.filters, relationshipFilter: this.relationshipFilter });
    }

    private changeFilterField(target, filter) {

        if (target.Type == FilterFieldType.Field) {
            filter.Data = new GridFilterExpression();
            filter.Data.field = target.Data.datafield;

            filter.Type = FilterFieldType.Field;

            if (target.Data.columntype == "dropdownlist") {
                filter.Data.condition = "EQUAL";
            } else {
                filter.Data.condition = "CONTAINS";
            }

            //determine the field type
            if (target.Data.hiddenfield) {
                filter.Data.fieldtype = GridFilterFieldType.Hidden;
            } else {
                filter.Data.fieldtype = GridFilterFieldType.Normal;
            }
        } else if (target.Type == FilterFieldType.Relationship) {
            filter.Data = new GridRelationshipFilterExpression();
            filter.Data.relationshipType = target.Data;
            filter.Type = FilterFieldType.Relationship;

            this.loadRelationshipValues(filter.Data.relationshipType);
        }
    }

    hasMultipleRelationships() {
        return this.internalFilters.filter(x => x.Type == FilterFieldType.Relationship).length > 1;
    }


    private addFilter() {
        this.internalFilters.push(new FilterExpression());
    }

    private getRelationshipTypes() {
        this.relationshipsService.getObjectRelations('RuleType', 1)//this.artifactType.ID
            .subscribe(result => {
                this.relationshipTypes = result;

                this.addRelationshipTypesToAvailable(this.relationshipTypes);
            });
    }

    private addRelationshipTypesToAvailable(relTypes) {
        for (let relationship of relTypes) {
            this.availableFilters.push({
                Data: relationship,
                Name: $localize`Relationship` + ` - ${relationship.TargetName}`,
                Type: FilterFieldType.Relationship
            });
        }

        if (this.relationshipFilter) {
            let indx = this.internalFilters.findIndex(x => x.Type == FilterFieldType.Relationship);

            if (indx >= 0 && indx < this.internalFilters.length) {
                this.internalFilters.splice(indx, 1);
            }

            this.internalFilters.push({
                Type: FilterFieldType.Relationship,
                Data: this.relationshipFilter,
                Field: this.availableFilters.filter(x => x.Type == FilterFieldType.Relationship && x.Data.IntersectTypeID == this.relationshipFilter.relationshipType.IntersectTypeID)[0],
            });
        }
    }

    private loadRelationshipValues(relationshipType: ObjectRelationship) {
        this.relationshipValues.splice(0, this.relationshipValues.length);

        this.relationshipsService.getRelatedObjects(relationshipType.TargetType, relationshipType.TargetTypeID, relationshipType.IntersectTypeID)
            .subscribe(
                result => {
                    for (let item of result) {
                        this.relationshipValues.push({ label: item.Name, value: item.ID });
                    }
                });
    }

    private addRelationshipFilter() {
        this.relationshipFilter = new GridRelationshipFilterExpression();
    }

    private removeFilter(filter: FilterExpression) {
        let index = this.internalFilters.indexOf(filter);

        this.internalFilters.splice(index, 1);
    }
}
