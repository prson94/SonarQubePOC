import { Input, Component, OnChanges, SimpleChange, ViewChild, ChangeDetectorRef, OnDestroy } from '@angular/core';
import { BaseComponent } from '../base.component';
import { RelationshipsService } from '../../../services/relationships.service';
import { RelationshipCount, RelationshipTypeUIModel } from '../../../models/relationship.model';
import { DynamicRelationshipGridComponent } from './dynamic-relationship-grid.component';
import { ResponsibilityTypeRelationPermission } from '../../../models/responsibility-type.model';
import { ObjectDetailService } from '../../../services/object-detail.service';
import { AssetService } from '../../../services/asset.service';
import { forkJoin, Subscription } from 'rxjs';
import * as _ from 'lodash';
import { CompanySettingsService } from '../../../services/settings.service';

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

    @Input() assetTypeUid: string;
    @Input() assetUid: string;

    @Input() count: number = 0;

    relationshipItems: RelationshipTypeUIModel[] = [];
    selected: RelationshipTypeUIModel;

    nonEditablePredicates: string[] = ["Inter-type Hierarchy", "Intra-type Hierarchy", "User Ownership", "Object Ownership"];

    readOnly: boolean = false;
    cardinalityShow: boolean = true;
    hasRelationships: boolean = false;
    showAddRelationship: boolean = false;
    showEmptyRelationshipTypes: boolean = true;
    hideDelete: boolean = true;
    queryString: string = "";
    public hasAdd: boolean;
    public hasFilterMode: boolean = true;

    loadDataSubs: Subscription;

    @ViewChild(DynamicRelationshipGridComponent, { static: false }) private relGrid: DynamicRelationshipGridComponent;

    constructor(
        private assetService: AssetService,
        private objectDetailService: ObjectDetailService,
        protected relationshipsService: RelationshipsService,
        protected settingsService: CompanySettingsService,
        private changeDetectorRef: ChangeDetectorRef) {
        super(settingsService);
    }

    ngOnDestroy(): void {
        if (this.relGrid) {
            this.relGrid.ngOnDestroy();
        }
        if (this.loadDataSubs) {
            this.loadDataSubs.unsubscribe();
        }
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        const hasApiParameterChanges = ('objectID' in changes || 'objectType' in changes);
        if (!hasApiParameterChanges) {
            return;
        }

        this.isLoading = true;
        this.changeDetectorRef.markForCheck();
        this.objectDetailService.getObject(this.objectID, this.objectType).subscribe((res) => {
            let uid: string = '';
            if (res["Uid"]) {
                uid = res["Uid"];
            }
            else {
                uid = res.UID;
            }

            this.assetService.getUIDetailsForAssetUID(uid).subscribe((asset) => {
                if (asset === null) {
                    this.assetUid = uid;
                    this.assetTypeUid = uid;
                }
                else {
                    this.assetUid = uid;
                    this.assetTypeUid = asset["AssetTypeUid"];
                }
                this.relationshipItems = [];
                this.load();
            });
        });
    }

    load(): void {
        this.isLoading = true;
        this.changeDetectorRef.markForCheck();

        if (!this.assetTypeUid || !this.assetUid) {
            return;
        }

        if (this.loadDataSubs) {
            this.loadDataSubs.unsubscribe();
        }
        var relationshipSub = this.relationshipsService.getRelationshipTypes(this.assetTypeUid);

        if (this.objectType === "ReferenceItemType") {
            var relationshipSub = this.relationshipsService.getRelationshipTypes(this.referenceListUid);
        }

        var countsSub = this.relationshipsService.getRelationshipsCountsForAsset(this.assetUid);

        this.loadDataSubs = forkJoin([relationshipSub, countsSub]).subscribe((res) => {
            this.isLoading = false;
            this.changeDetectorRef.markForCheck();
            var allItems = res[0] as RelationshipTypeUIModel[];
            var counts = res[1] as RelationshipCount[];

            if (this.objectType === 'Task') {
                //hide relationship types of predicate type 'Diagram' when we are on Diagram asset relationship screen
                allItems = allItems.filter((rel) => rel.Predicate.Type !== 'Diagram');
            }

            this.ProcessRelationshipTypesResponse(allItems, counts);
        });

        this.permissions = this.objectPermissions;
    }

    private ProcessRelationshipTypesResponse(allItems: RelationshipTypeUIModel[], counts: RelationshipCount[]) {

        this.selected = null;

        var origLength = allItems.length;
        for (let i = 0; i < origLength; i++) {
            allItems[i].IsSubject = allItems[i].Subject.Uid === this.assetTypeUid;

            if (allItems[i].Subject.Uid === allItems[i].Object.Uid) {
                var copy = _.cloneDeep(allItems[i]);
                copy.IsSubject = !allItems[i].IsSubject;
                allItems.push(copy);
            }
        }

        for (let relation of allItems) {
            var count = counts.filter((f) => f.IntersectTypeUid === relation.Uid && f.IsSubject === relation.IsSubject);
            if (count.length !== 0) {
                relation.Count = count[0].Count;
            }
            else {
                relation.Count = 0;
            }

            relation.AllowEditFromRelationshipEditor = this.nonEditablePredicates.indexOf(relation.Predicate.Type) === -1;
            this.relationshipItems.push(relation);
        }

        for (let relation of this.relationshipItems) {
            relation.TypeName = this.getRelName(relation);
        }

        this.relationshipItems = this.relationshipItems.sort((a, b) => { return a.TypeName > b.TypeName ? 1 : -1; });

        for (let relation of this.relationshipItems) {
            if (relation.Count > 0 && !this.selected) {
                this.selected = relation;
            }
        }

        if (!this.selected)
            this.selected = (this.relationshipItems && this.relationshipItems.length > 0) ? this.relationshipItems[0] : null;

        this.hasRelationships = (this.relationshipItems && this.relationshipItems.length > 0);

        this.updateCardinality();
        this.changeDetectorRef.detectChanges();
    }

    export() {
        if (!this.selected) return;
        var params = {};
        if (this.selected.IsSubject) {
            params["subjectUid"] = this.assetUid;
        }
        else {
            params["objectUid"] = this.assetUid;
        }

        this.relationshipsService.getRelationships(this.selected.Uid, params, true).subscribe();
    }

    addRelationship(event: any) {
        var uid = event.uid;
        var isSubject = event.isSubject;
        var item = this.relationshipItems.filter((r) => r.Uid === uid && r.IsSubject === isSubject)[0];

        var count = (event.data as any[]).length;
        item.Count = item.Count += count;
        this.updateCardinality();
    }

    removeRelationship(event: any) {
        var uid = event.uid;
        var isSubject = event.isSubject;
        var item = this.relationshipItems.filter((r) => r.Uid === uid && r.IsSubject === isSubject)[0];

        item.Count = item.Count - 1;
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
        this.showAddRelationship = false;
        this.selected = rel;
        this.updateCardinality();
    }
    private updateCardinality() {
        if (this.selected != null) {

            var cardinality = this.selected.IsSubject ? this.selected.Object.Cardinality : this.selected.Subject.Cardinality;

            this.cardinalityShow = (cardinality === "Many") || (this.selected.Count === 0 && cardinality !== "Many");

            this.readOnly = this.selected.Predicate.Type === "InterTypeHierarchy"
                || this.selected.Predicate.Type === "IntraTypeHierarchy";

            this.hasAdd = this.cardinalityShow
                && this.hasRelationships
                && this.selected
                && this.hasAddRelationshipsPermissions()
                && !this.readOnly
                && this.selected.AllowEditFromRelationshipEditor;
        }
        else {
            this.hasAdd = this.hasRelationships && this.hasAddRelationshipsPermissions() && !this.readOnly;
        }
        this.hasFilterMode = this.hasRelationships;
    }

    getRelName(rel: RelationshipTypeUIModel) {
        var correctSide = rel.IsSubject ? rel.Object : rel.Subject;
        return `${correctSide.Name} [${(!rel.IsSubject ? rel.Predicate.Inverse : rel.Predicate.Name)}]`;
    }

    getIconClass(rel: RelationshipTypeUIModel) {
        var isObject = rel.Object.Uid === this.assetTypeUid;
        if (this.objectType === "ReferenceItemType" && this.isReferenceListType(rel.Object.Uid)) {
            isObject = true;
        }
        if (this.objectType === "ReferenceItemType" && this.isReferenceListType(rel.Subject.Uid)) {
            isObject = false;
        }

        var type = isObject ? rel.Subject.Class : rel.Object.Class;

        let cs: string = 'fa inactive-tool-icon ';

        switch (type) {
            case "Rule":
                cs += "fa-pie-chart";
                break;
            case "Policy":
                cs += "fa-university";
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
