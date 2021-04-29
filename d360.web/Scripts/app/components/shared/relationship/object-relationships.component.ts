import { Input, Output, Component, OnChanges, SimpleChange, ViewChild, ChangeDetectorRef, OnDestroy } from '@angular/core';
import { BaseComponent } from '../base.component';
import { RelationshipsService } from '../../../services/relationships.service';
import { ObjectRelationshipCount, RelationshipType, RelationshipTypeUIModel } from '../../../models/relationship.model';
import { DynamicRelationshipGridComponent } from './dynamic-relationship-grid.component';
import { ResponsibilityTypeRelationPermission } from '../../../models/responsibility-type.model';

@Component({
    selector: 'd3s-object-relationships',
    providers: [RelationshipsService],
    templateUrl: './object-relationships.component.html'
})

export class ObjectRelationshipsComponent extends BaseComponent implements OnChanges, OnDestroy {
    @Input() objectType: string;
    @Input() objectID: number;
    @Input() objectName: string;
    @Input() objectPermissions: ResponsibilityTypeRelationPermission[] = [];
    @Input() isModal: boolean = false;


    @Input() assetTypeUid: string = '36226286-e3b5-48b9-bb8f-7b149c8a5d63';

    relationshipItems: RelationshipTypeUIModel[] = [];
    selected: RelationshipTypeUIModel;

    readOnly: boolean = false;
    cardinalityShow: boolean = true;
    hasRelationships: boolean = false;
    showAddRelationship: boolean = false;
    showEmptyRelationshipTypes: boolean = true;
    hideDelete: boolean = true;
    queryString: string = "";
    public hasAdd: boolean;
    public hasFilterMode: boolean = true;

    @ViewChild(DynamicRelationshipGridComponent, { static: false }) private relGrid: DynamicRelationshipGridComponent;

    constructor(protected relationshipsService: RelationshipsService, private changeDetectorRef: ChangeDetectorRef) {
        super();
    }

    ngOnDestroy(): void {
        this.relGrid.ngOnDestroy();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        this.load();
    }

    load(): void {
        this.isLoading = true;

        if (this.objectType == null || this.objectID == null)
            return;

        this.relationshipsService.getRelationshipTypes(this.assetTypeUid).subscribe((res) => {
            this.relationshipItems = res as RelationshipTypeUIModel[];

            this.selected = null;
            for (let relation of this.relationshipItems) {
                relation.Count = 0;
                if (relation.Count > 0) {
                    this.selected = relation;
                    break;
                }
            }

            if (!this.selected)
                this.selected = (this.relationshipItems && this.relationshipItems.length > 0) ? this.relationshipItems[0] : null;

            this.hasRelationships = (this.relationshipItems && this.relationshipItems.length > 0);

            this.isLoading = false;
            this.updateCardinality();
            this.changeDetectorRef.markForCheck();
        });

        this.permissions = this.objectPermissions;
        return;


        this.loadRelationshipItems();
    }

    loadRelationshipItems() {

        //this.relationshipsService.getRelationshipCounts(this.objectType, this.objectID)
        //    .subscribe(result => {
        //        this.relationshipItems = result;
        //        this.selected = null;
        //        for (let relation of this.relationshipItems) {
        //            if (relation.Count > 0) {
        //                this.selected = relation;
        //                break;
        //            }
        //        }

        //        if (!this.selected)
        //            this.selected = (this.relationshipItems && this.relationshipItems.length > 0) ? this.relationshipItems[0] : null;

        //        this.hasRelationships = (this.relationshipItems && this.relationshipItems.length > 0);

        //        this.isLoading = false;
        //        this.updateCardinality();
        //        this.changeDetectorRef.markForCheck();
        //    }
        //    );
    }

    export() {
        if (!this.selected) return;
        //    this.relationshipsService.exportObjectRelationshipsToExcel(this.objectType, this.objectID, this.selected.Object, this.selected.ObjectID, this.selected.IntersectTypeID, this.queryString, false);
    }

    addRelationship(event) {
        if (!this.selected) return;
        this.selected.Count = event.count;
        this.updateCardinality();
    }

    removeRelationship() {
        if (!this.selected) return;
        this.selected.Count--;
        this.updateCardinality();
    }

    hideforDelete() {
        this.hideDelete = false;
    }

    unhideforDelete() {
        this.hideDelete = true;
    }

    enableExport() {
        if (!this.selected) return false;
        return this.selected.Count > 0;
    }

    isSelected(item: RelationshipTypeUIModel): boolean {
        return (this.selected && this.selected == item);
    }

    relationshipsToShow() {
        if (this.showEmptyRelationshipTypes)
            return this.relationshipItems;

        return this.relationshipItems.filter((x) => x.Count > 0);
    }

    onFilterChange(qstring) {
        this.queryString = qstring;
    }

    public relationClick(rel: any) {

        this.selected = rel;
        this.updateCardinality();
    }
    private updateCardinality() {
        //if (this.selected != null) {
        //    this.cardinalityShow = (this.selected.Cardinality == 2) || (this.selected.Count == 0 && this.selected.Cardinality != 2);
        //    this.hasAdd = this.cardinalityShow
        //        && this.hasRelationships
        //        && this.selected
        //        && this.hasModifyRelationshipsPermissions()
        //        && !this.readOnly
        //        && this.selected.AllowEditFromRelationshipEditor;
        //}
        //else {
        //    this.hasAdd = this.hasRelationships && this.hasAddRelationshipsPermissions() && !this.readOnly;
        //}
        //this.hasFilterMode = this.hasRelationships;
    }

    getRelName(rel: RelationshipTypeUIModel) {
        var isObject = rel.Object.Uid == this.assetTypeUid;

        var correctSide = isObject ? rel.Subject : rel.Object;
        return `${correctSide.Name} [${(isObject ? rel.Predicate.Inverse : rel.Predicate.Name)}]`;
    }
}
