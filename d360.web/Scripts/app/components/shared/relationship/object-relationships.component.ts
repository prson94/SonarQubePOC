import { Input, Output, Component, OnChanges, SimpleChange, ViewChild, ChangeDetectorRef, OnDestroy } from '@angular/core';
import { BaseComponent } from '../base.component';
import { RelationshipsService } from '../../../services/relationships.service';
import { RelationshipTypeUIModel } from '../../../models/relationship.model';
import { DynamicRelationshipGridComponent } from './dynamic-relationship-grid.component';
import { ResponsibilityTypeRelationPermission } from '../../../models/responsibility-type.model';
import { ObjectDetailService } from '../../../services/object-detail.service';
import { AssetService } from '../../../services/asset.service';

@Component({
    selector: 'd3s-object-relationships',
    providers: [RelationshipsService, ObjectDetailService, AssetService],
    templateUrl: './object-relationships.component.html'
})

export class ObjectRelationshipsComponent extends BaseComponent implements OnChanges, OnDestroy {
    @Input() objectType: string;
    @Input() objectID: number;
    @Input() objectName: string;
    @Input() objectPermissions: ResponsibilityTypeRelationPermission[] = [];
    @Input() isModal: boolean = false;

    isSubject: boolean = false;

    @Input() assetTypeUid: string;
    @Input() assetUid: string;

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

    constructor(protected relationshipsService: RelationshipsService,
        private objectDetailService: ObjectDetailService,
        private assetService: AssetService,
        private changeDetectorRef: ChangeDetectorRef) {
        super();
    }

    ngOnDestroy(): void {
        this.relGrid.ngOnDestroy();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        this.objectDetailService.getObject(this.objectID, this.objectType).subscribe((res) => {
            this.assetService.getUIDetailsForAssetUID(res["Uid"]).subscribe((asset) => {
                this.assetUid = res["Uid"];
                this.assetTypeUid = asset["AssetTypeUid"];
                this.load();
            });
        });
    }

    load(): void {
        this.isLoading = true;

        if (!this.assetTypeUid || !this.assetUid)
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
        if (this.selected != null) {
            this.isSubject = this.selected.Subject.Uid == this.assetTypeUid;

            var cardinality = this.isSubject ? this.selected.Object.Cardinality : this.selected.Subject.Cardinality;

            this.cardinalityShow = (cardinality === "Many") || (this.selected.Count == 0 && cardinality !== "Many");
            this.hasAdd = this.cardinalityShow
                && this.hasRelationships
                && this.selected
                && this.hasModifyRelationshipsPermissions()
                && !this.readOnly
                && this.selected.AllowEditFromRelationshipEditor;
        }
        else {
            this.hasAdd = this.hasRelationships && this.hasAddRelationshipsPermissions() && !this.readOnly;
        }
        this.hasFilterMode = this.hasRelationships;
    }

    getRelName(rel: RelationshipTypeUIModel) {
        var isObject = rel.Object.Uid == this.assetTypeUid;

        var correctSide = isObject ? rel.Subject : rel.Object;
        return `${correctSide.Name} [${(isObject ? rel.Predicate.Inverse : rel.Predicate.Name)}]`;
    }

    getIconClass(rel: RelationshipTypeUIModel) {
        {
            var isObject = rel.Object.Uid == this.assetTypeUid;
            var type = isObject ? rel.Subject.Class : rel.Object.Class;

            let cs: string = 'fa inactive-tool-icon ';

            switch (type) {
                case "Rule":
                    cs += "fa-pie-chart";
                    break;
                case "Policy":
                    cs += "fa-university";
                    break;
                case "FusionAttribute":
                    cs += "fa-database";
                    break;
                case "Resource":
                    cs += "fa-user";
                    break;
                case "ReferenceItemType":
                case "Reference":
                    cs += "fa-list";
                    break;
                case "Diagram":
                    cs += "fa-share-alt";
                    break;
                case "BusinessAsset":
                case "TechnicalAsset":
                    cs += "fa-book";
                    break;
                default:
                    cs += "fa-book";
                    break;
            }
            return cs;
        }
    }

    getIconTooltip(rel: RelationshipTypeUIModel) {
        {
            var isObject = rel.Object.Uid == this.assetTypeUid;
            var type = isObject ? rel.Subject.Class : rel.Object.Class;

            let tooltip: string = '';

            switch (type) {
                case "Rule":
                    tooltip = "Rule";
                    break;
                case "Policy":
                    tooltip = "Policy";
                    break;
                case "FusionAttribute":
                    tooltip = "Fusion Attribute";
                    break;
                case "Resource":
                    tooltip = "Resource";
                    break;
                case "ReferenceItemType":
                case "Reference":
                    tooltip = "Reference List";
                    break;
                case "Diagram":
                    tooltip = "Diagram";
                    break;
                case "BusinessAsset":
                    tooltip = "Business Asset";
                    break;
                case "TechnicalAsset":
                    tooltip = "Technical Asset";
                    break;
                default:
                    tooltip = "";
                    break;
            }
            return tooltip;
        }
    }
}
