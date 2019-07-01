import {
    ChangeDetectorRef,
    Component,
    EventEmitter,
    Input,
    OnChanges,
    OnInit,
    Output,
    SimpleChange
} from '@angular/core';
import {SelectItem} from 'primeng/primeng';
import {RelationshipsService} from '../../services/relationships.service';
import {AttributeTypeService} from '../../services/attribute-type.service';
import {ArtifactTypeService} from '../../services/artifact-type.service';
import {ArtifactType} from '../../models/artifact-type.model';
import {
    GridAttributeFilterExpression,
    GridFilterColumn,
    GridFilterExpression,
    GridFilterFieldType,
    GridOwnerFilter,
    GridRelationshipFilterExpression
} from '../../models/grid-definition.model';
import {ObjectRelationship} from '../../models/relationship.model';
import {AttributeType} from '../../models/attribute-type.model';
import {FilterExpression, FilterField, FilterFieldType} from '../../models/filter-field.model';
import { map } from 'rxjs/operators';
import { Observable } from 'rxjs';

@Component({
    selector: 'd3s-artifact-column-filter',
    providers: [RelationshipsService, AttributeTypeService, ArtifactTypeService],
    styles: [`
        div.filter {
            padding-bottom: 5px;
        }

        div.buttons {
            padding-left: 10px;
            padding-bottom: 5px;
        }
    `],
    templateUrl: './artifact-column-filter.component.html'
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
    connectors: SelectItem[] = [{label: "And", value: "All"}, {label: "Or", value: "Any"}];
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

    constructor(
        private relationshipsService: RelationshipsService,
        private attributeTypeService: AttributeTypeService,
        private artifactTypeService: ArtifactTypeService,
        private ref: ChangeDetectorRef
    ) {
    }

    ngOnInit() {
        if (this.attributeFilters.length > 0) {
            this.attributeSelected(this.attributeFilters[0].attributeType);
        }

        if (this.ownerFilter && (this.ownerFilter.ownerGroups || this.ownerFilter.ownerUsers)) {
            this.ownerSelected();
        }
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        var bHasInternalFilters = this.internalFilters.filter(x => x.Type == FilterFieldType.Field).length > 0;

        if (changes["fields"] && this.fields != null && this.fields.length > 0) {
            this.availableFilters = [];
            for (let field of this.fields) {
                this.availableFilters.push({
                    Data: field, Name: `${field.text}`, Type: FilterFieldType.Field
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
            } else if (this.relationshipFilters.length > 0 && !bHasInternalFilters) {
                // dont clear the relationship filters
            } else if (this.attributeFilters && this.attributeFilters.length == 0 && !this.ownerFilter && this.relationshipFilters
                && this.relationshipFilters.length == 0 && this.filters && this.filters.length == 0 && this.internalFilters.length == 0) {

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
            } else if (internalFilter.Type == FilterFieldType.Attribute) {
                this.attributeFilters.push(internalFilter.Data);
            } else if (internalFilter.Type == FilterFieldType.Relationship) {
                this.relationshipFilters.push(internalFilter.Data);
            } else if (internalFilter.Type == FilterFieldType.Owner) {
                this.ownerFilter = new GridOwnerFilter();
                this.ownerFilter.ownerUsers = [];
                this.ownerFilter.ownerGroups = [];
                for (let owner of internalFilter.Data) {
                    if (owner.Type.toUpperCase() == 'RESOURCE') {
                        this.ownerFilter.ownerUsers.push(owner.ID);
                    } else {
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

        this.filterChanged.emit({filter: this.filters, relationshipFilter: this.relationshipFilters});
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
                    this
                        .artifactTypeService
                        .getFilterListItems(this.artifactType.ID, 'ArtifactType', fieldId)
                        .subscribe(r => {
                            filter.Field.Data.filteritems = r;
                            this.isLoadingFilter = false;
                            this.ref.markForCheck();
                        });
                }
            } else if (target.Data.filtertype == 'list' && target.Data.datafield != null && target.Data.datafield.toLowerCase() == 'parent') {
                this.isLoadingFilter = true;
                this
                    .artifactTypeService
                    .getObjectTypeParentsListItems(this.artifactType.ID, 'ArtifactType')
                    .subscribe(r => {
                        filter.Field.Data.filteritems = r;
                        this.isLoadingFilter = false;
                        this.ref.markForCheck();
                    });
            }

            if (target.Data.columntype == "dropdownlist" || target.Data.columntype == "numberinput") {
                filter.Data.condition = "EQUAL";
            } else {
                filter.Data.condition = "CONTAINS";
            }

            //determine the field type
            if (target.Data.hiddenfield) {
                filter.Data.fieldtype = GridFilterFieldType.Hidden;
            } else if (target.Data.relatedfield) {
                filter.Data.fieldtype = GridFilterFieldType.Relation;
            } else {
                filter.Data.fieldtype = GridFilterFieldType.Normal;
            }
        } else if (target.Type == FilterFieldType.Relationship) {
            filter.Data = new GridRelationshipFilterExpression();
            filter.Data.relationshipType = target.Data;
            filter.Type = FilterFieldType.Relationship;

            this.loadRelationshipValues(filter.Data).subscribe();
        } else if (target.Type == FilterFieldType.Attribute) {
            filter.Data = new GridAttributeFilterExpression();
            filter.Data.attributeType = target.Data.ID;
            filter.Type = FilterFieldType.Attribute;

            this.attributeSelected(target.Data.ID);
        } else if (target.Type == FilterFieldType.Owner) {
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
        if (this.ownerValues.length > 0) {
            /* already loaded owners */
            return;
        }

        this
            .artifactTypeService
            .getPossibleArtifactOwners(this.artifactType.ID)
            .subscribe(result => {
                for (let item of result) {
                    this.ownerValues.push({label: item.Name, value: item});
                }

                //add an internal filter in case we need to init the ui this way
                if (this.ownerFilter) {
                    var owners = [];
                    var filter = new FilterExpression();

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
        this
            .attributeTypeService
            .getAttributeFilterValues(
                'ArtifactType',
                this.artifactType.ID, target
            )
            .subscribe(result => {
                this.attributeValues = result;
            });
    }

    private getAttributes() {
        try {
            if (!this.artifactType || this.artifactType.ID <= 0) return;

            this
                .attributeTypeService
                .getAttributeTypesForObject(
                    'ArtifactType',
                    this.artifactType.ID
                )
                .subscribe(result => {
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
        } catch (e) {
            console.log("Error: " + e);
        }
    }

    //#endregion

    //#region Relationship Logic

    private loadRelationshipValues(expr: GridRelationshipFilterExpression): Observable<any> {
        return this.relationshipsService
            .getRelatedObjects(expr.relationshipType.TargetType, expr.relationshipType.TargetTypeID, expr.relationshipType.IntersectTypeID)
            .pipe(
                map(result => {
                expr.options = [];
                for (let item of result) {
                    expr.options.push({label: item.Name, value: item.ID});
                }
                this.ref.markForCheck();
            }));
    }

    private addRelationshipTypesToAvailable(relTypes) {
        for (let relationship of relTypes) {
            this.availableFilters.push({
                Data: relationship,
                Name: `${relationship.TargetName}${relationship.PredicateName ? '(' + relationship.PredicateName + ')' : ''}`,
                Type: FilterFieldType.Relationship
            });
        }

        if (this.relationshipFilters) {
            this.internalFilters = this.internalFilters.filter(x => x.Type != FilterFieldType.Relationship);

            for (let rel of this.relationshipFilters) {
                this.loadRelationshipValues(rel).subscribe(() => {
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
                    .subscribe(result => {
                        this.relationshipTypes = result;
                        this.addRelationshipTypesToAvailable(this.relationshipTypes);
                        this.ref.markForCheck();
                    });
            } else {
                this.addRelationshipTypesToAvailable(this.relationshipTypes);
                this.ref.markForCheck();
            }
        } catch (e) {
            console.log("Error: " + e);
        }
    }

    //#endregion

    private removeFilter(filter: FilterExpression) {
        let index = this.internalFilters.indexOf(filter);
        this.internalFilters.splice(index, 1);
    }
};
