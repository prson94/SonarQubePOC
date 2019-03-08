import { Input, Output, Component, OnChanges, SimpleChange, ViewChild } from '@angular/core';
import { BaseComponent } from '../base.component';
import { RelationshipsService } from '../../../services/relationships.service';
import { ObjectRelationshipCount } from '../../../models/relationship.model';
import { DynamicRelationshipGridComponent } from './dynamic-relationship-grid.component';
import { ResponsibilityTypeRelationPermission } from '../../../models/responsibility-type.model';

@Component({
    selector: 'd3s-object-relationships',
    providers: [RelationshipsService],
    templateUrl: './object-relationships.component.html'
})

export class ObjectRelationshipsComponent extends BaseComponent implements OnChanges {
    @Input() objectType: string;
    @Input() objectID: number;
    @Input() objectName: string;
    @Input() objectPermissions: ResponsibilityTypeRelationPermission[] = [];

    relationshipItems: ObjectRelationshipCount[] = [];
    selected: ObjectRelationshipCount;

    readOnly: boolean = false;
    cardinalityShow: boolean = true;
    hasRelationships: boolean;
    showAddRelationship: boolean = false;
    showEmptyRelationshipTypes: boolean = true;
    hideDelete: boolean = true;
    queryString: string = "";
    public hasAdd: boolean;

    @ViewChild(DynamicRelationshipGridComponent) private relGrid: DynamicRelationshipGridComponent;

    constructor(protected relationshipsService: RelationshipsService) {
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
                this.selected = null;
                for (let relation of this.relationshipItems) {
                    if (relation.Count > 0) {
                        this.selected = relation;
                        break;
                    }
                }

                if (!this.selected) this.relationshipItems.length > 0 ? this.relationshipItems[0] : null;

                this.hasRelationships = (this.relationshipItems && this.relationshipItems.length > 0);

                this.isLoading = false;
                this.updateCardinality();
            });
    }

    export() {
        if (!this.selected) return;
        this.relationshipsService.exportObjectRelationshipsToExcel(this.objectType, this.objectID, this.selected.Object, this.selected.ObjectID, this.selected.IntersectTypeID, this.queryString, false);
    }

    addRelationship(event) {
        if (!this.selected) return;
        this.selected.Count = this.selected.Count + event.count;
        this.updateCardinality();
    }

    removeRelationship() {
        if (!this.selected) return;
        this.selected.Count--;
        this.updateCardinality();
    }

    hideforDelete() {
        this.hideDelete = false;
        console.log("hideforDelete");
    }

    unhideforDelete() {
        this.hideDelete = true;
        console.log("unhideforDelete");
    }

    enableExport() {
        console.log("enableExport");
        if (!this.selected) return false;
        return this.selected.Count > 0;
    }

    isSelected(item: ObjectRelationshipCount): boolean {
        console.log("isSelected");
        return (this.selected && this.selected == item);
    }

    relationshipsToShow() {
        console.log("relationshipsToShow");
        if (this.showEmptyRelationshipTypes)
            return this.relationshipItems;

        return this.relationshipItems.filter(x => x.Count > 0);
    }

    onFilterChange(qstring) {
        this.queryString = qstring;
        console.log("onFilterChange");
    }

    public relationClick(rel: any) {

        this.selected = rel;
        this.updateCardinality();
    }
    private updateCardinality() {
        if (this.selected != null) {
            this.cardinalityShow = (this.selected.Cardinality == 2) || (this.selected.Count == 0 && this.selected.Cardinality != 2);
            this.hasAdd = this.cardinalityShow
                && this.hasRelationships
                && this.selected
                && this.hasModifyRelationshipsPermissions()
                && !this.readOnly
                && this.selected.AllowEditFromRelationshipEditor;
        }
        else {
            this.hasAdd = this.hasModifyRelationshipsPermissions() && !this.readOnly;
        }
    }
}
