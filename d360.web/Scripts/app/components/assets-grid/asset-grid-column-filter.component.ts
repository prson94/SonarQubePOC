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
import { SelectItem } from 'primeng/api';
import { RelationshipsService } from '../../services/relationships.service';
import { ArtifactTypeService } from '../../services/artifact-type.service';
import { ArtifactType } from '../../models/artifact-type.model';
import {
    GridFilterColumn,
    GridFilterExpression,
    GridFilterFieldType,
    GridOwnerFilter,
    GridRelationshipFilterExpression
} from '../../models/grid-definition.model';
import { ObjectRelationship } from '../../models/relationship.model';
import { FilterExpression, FilterField, FilterFieldType } from '../../models/filter-field.model';
import { map } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { AssetGridObject } from './asset-grid.model';
import { AssetService } from '../../services/asset.service';
import { AssetTypeService } from '../../services/asset-type.service';

@Component({
    selector: 'd3s-asset-grid-column-filter',
    providers: [RelationshipsService, ArtifactTypeService, AssetTypeService],
    styles: [`
        div.filter {
            padding-bottom: 5px;
        }

        div.buttons {
            padding-left: 10px;
            padding-bottom: 5px;
        }
    `],
    templateUrl: './asset-grid-column-filter.component.html'
})

export class AssetGridColumnFilterComponent implements OnInit, OnChanges {
    @Input() fields: GridFilterColumn[];
    @Input() gridObject: AssetGridObject;
    @Input() objectType: string = 'ArtifactType';
    @Output() filterChanged = new EventEmitter();

    @Input() filters: GridFilterExpression[] = [];
    @Output() filtersChange = new EventEmitter();

    @Input() relationshipFilters: GridRelationshipFilterExpression[] = [];
    @Output() relationshipFiltersChange = new EventEmitter();

    @Input() ownerFilter: GridOwnerFilter = null;
    @Output() ownerFilterChange = new EventEmitter();

    relationshipTypes: ObjectRelationship[];
    connectors: SelectItem[] = [{ label: "And", value: "All" }, { label: "Or", value: "Any" }];
    ownerValues: SelectItem[] = [];

    filterFieldType = FilterFieldType;

    internalFilters: FilterExpression[] = [];
    private availableFilters: FilterField[] = [];
    private isLoadingFilter = false;
    private ownerShipFilter: FilterField = {
        Data: null,
        Name: 'Owned by',
        Type: FilterFieldType.Owner
    };

    constructor(
        private relationshipsService: RelationshipsService,
        private artifactTypeService: ArtifactTypeService,
        private assetTypeService: AssetTypeService,
        private ref: ChangeDetectorRef
    ) {
    }

    ngOnInit() {
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
            } else if (!this.ownerFilter && this.relationshipFilters
                && this.relationshipFilters.length == 0 && this.filters && this.filters.length == 0 && this.internalFilters.length == 0) {

                this.resetFilters();
            }

            this.getRelationships();
            this.availableFilters.push(this.ownerShipFilter);
        }
    }

    onSubmit() {
        let hasOwnerFilter = false;

        this.filters = [];
        this.relationshipFilters = [];
        this.ownerFilter = null;

        let usedFilters = [];
        let filterIndicesToRemove = [];

        for (let internalFilter of this.internalFilters) {

            let currentIndex = this.internalFilters.indexOf(internalFilter);

            if (internalFilter.Type == FilterFieldType.Field) {
                if (usedFilters.indexOf(internalFilter.Data.field) !== -1 && !internalFilter.Field.Data.canHaveMultipleFilters) {
                    filterIndicesToRemove.push(currentIndex);
                }
                else {
                    this.filters.push(internalFilter.Data);
                    usedFilters.push(internalFilter.Data.field);
                }
            } else if (internalFilter.Type == FilterFieldType.Relationship) {
                if (usedFilters.indexOf("R" + internalFilter.Data.relationshipType.IntersectTypeID) !== -1) {
                    filterIndicesToRemove.push(currentIndex);
                }
                else {
                    this.relationshipFilters.push(internalFilter.Data);
                    usedFilters.push("R" + internalFilter.Data.relationshipType.IntersectTypeID);
                }
            } else if (internalFilter.Type == FilterFieldType.Owner) {
                if (usedFilters.indexOf("O") !== -1) {
                    filterIndicesToRemove.push(currentIndex);
                }
                else {
                    this.ownerFilter = new GridOwnerFilter();
                    this.ownerFilter.ownerUsers = [];
                    this.ownerFilter.ownerGroups = [];
                    for (let owner of internalFilter.Data) {
                        if (owner.Type.toUpperCase() == 'RESOURCE') {
                            this.ownerFilter.ownerUsers.push(owner.Uid);
                        } else {
                            this.ownerFilter.ownerGroups.push(owner.Uid);
                        }
                    }
                    hasOwnerFilter = true;
                    usedFilters.push("O");
                }
            }
        }

        if (!hasOwnerFilter && this.ownerFilter) this.ownerFilter = null;

        // Remove duplicate filters based on field searched. Only leave the first instance of each filter.
        if (filterIndicesToRemove.length > 0) {
            for (let ix of filterIndicesToRemove) {
                this.internalFilters.splice(ix, 1);
            }
        }

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
                    this
                        .artifactTypeService
                        .getFilterListItems(this.gridObject.ID, this.objectType, fieldId)
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
                    .getObjectTypeParentsListItems(this.gridObject.ID, this.objectType)
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
            } else {
                filter.Data.fieldtype = GridFilterFieldType.Normal;
            }
        } else if (target.Type == FilterFieldType.Relationship) {
            filter.Data = new GridRelationshipFilterExpression();
            filter.Data.relationshipType = target.Data;
            filter.Type = FilterFieldType.Relationship;

            this.loadRelationshipValues(filter.Data).subscribe();
        } else if (target.Type == FilterFieldType.Owner) {
            filter.Data = [];
            filter.Type = FilterFieldType.Owner;

            this.ownerSelected();
        }
    }

    hasMultipleOwners() {
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
        this.assetTypeService.GetAssetTypePossibleOwners(this.gridObject.AssetTypeUID)
            .subscribe(result => {
                for (let item of result) {
                    this.ownerValues.push({ label: item.Name, value: item });
                }

                //add an internal filter in case we need to init the ui this way
                if (this.ownerFilter) {
                    var owners = [];
                    var filter = new FilterExpression();

                    for (let group of this.ownerFilter.ownerGroups) {
                        //find a group in results with type group and id matching
                        let indx = result.findIndex(x => x.Uid == group && x.Type == "Group");

                        if (indx >= 0 && indx < result.length) {
                            owners.push(result[indx]);
                        }
                    }

                    for (let user of this.ownerFilter.ownerUsers) {
                        let indx = result.findIndex(x => x.Uid == user && x.Type == "Resource");

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

    //#region Relationship Logic

    private loadRelationshipValues(expr: GridRelationshipFilterExpression): Observable<any> {
        return this.relationshipsService
            .getRelatedObjects(expr.relationshipType.TargetType, expr.relationshipType.TargetTypeID, expr.relationshipType.IntersectTypeID)
            .pipe(
                map(result => {
                    expr.options = [];
                    for (let item of result) {
                        expr.options.push({ label: item.Name, value: item.Uid });
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
                if (!this.gridObject || this.gridObject.ID <= 0) return;

                this.relationshipsService
                    .getObjectRelations(this.objectType, this.gridObject.ID)
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
