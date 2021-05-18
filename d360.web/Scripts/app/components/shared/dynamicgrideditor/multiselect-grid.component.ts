import { Input, Component, Output, EventEmitter, OnInit, forwardRef, ChangeDetectionStrategy, ChangeDetectorRef, OnChanges } from '@angular/core';
import { BaseComponent } from '../base.component';
import { UriBasedService } from '../../../services/uri-based.service';
import * as _ from 'lodash';
import { NG_VALUE_ACCESSOR, ControlValueAccessor } from '@angular/forms';
import { LazyLoadEvent } from 'primeng/api';
import { AssetService } from '../../../services/asset.service';
import { forkJoin, Subscription } from 'rxjs';
import { AssetTypeService } from '../../../services/asset-type.service';
import { AssetTypeClass } from '../../../models/asset.model';
import { RelationshipsService } from '../../../services/relationships.service';
import { RelationshipType } from '../../../models/relationship.model';
import { EditorField } from '../../../models/editor-field.model';

export const MULTISELECT_GRID_VALUE_ACCESSOR: any = {
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => MultiSelectGridComponent),
    multi: true
};

@Component({
    selector: 'd3s-multiselect-grid',
    templateUrl: "multiselect-grid.component.html",
    providers: [MULTISELECT_GRID_VALUE_ACCESSOR, AssetService, AssetTypeService],
    changeDetection: ChangeDetectionStrategy.OnPush,
})

export class MultiSelectGridComponent extends BaseComponent implements ControlValueAccessor, OnInit {
    @Input() multiple: boolean = true;
    @Input() intersectTypeUid: string;
    @Input() assetUid: string;
    @Input() targetAssetTypeUid: string;
    @Input() objectCardinality: string;

    relationshipType: RelationshipType;
    isSubject: boolean = true;

    value: any; //stores the values array bound back to the ngform.

    items: any[];
    selectedItems: any;
    private selectedRelationRowIndex: number = null;

    public onModelChange: Function = () => { };

    public onModelTouched: Function = () => { };

    lazyLoadTotalCount: number = 0;

    searchAssetSub: Subscription;

    constructor(
        private assetService: AssetService,
        private assetTypeService: AssetTypeService,
        private relationshipService: RelationshipsService,
        private ref: ChangeDetectorRef) {
        super();
    }

    get isLazyLoad() {
        //we are not using lazy load on assets api only in case when our relationship is from or to Reference List
        //this should be handled separately as we are dealing with asset type here, not asset
        return !this.isReferenceListType(this.targetAssetTypeUid);
    }

    ngOnInit() {
        this.assetService.getUIDetailsForAssetUID(this.assetUid)
            .subscribe((ad) => {

                this.relationshipService.getRelationshipTypes(ad.AssetTypeUid).subscribe((rel) => {
                    this.relationshipType = rel.filter((r) => r.Uid === this.intersectTypeUid)[0];
                    this.isSubject = this.relationshipType.Subject.Uid === ad.AssetTypeUid;
                    this.ref.markForCheck();
                    if (!this.isLazyLoad) {
                        this.loadReferenceListTypeData();
                    }
                });
            });
    }

    loadReferenceListTypeData() {
        this.isLoading = true;

        var params = {};
        if (this.isSubject) {
            params["subjectUid"] = this.assetUid;
            params["_order"] = "object.[path]";
        }
        else {
            params["objectUid"] = this.assetUid;
            params["_order"] = "subject.[path]";
        }

        params["_includePath"] = true;
        params["_pageSize"] = 10000;
        params["_pageNum"] = 1;
        forkJoin(
            this.relationshipService.getRelationships(this.intersectTypeUid, params),
            this.assetTypeService.getAssetTypesByClass(AssetTypeClass.Reference)
        ).subscribe((results) => {
            var relations = results[0].items as RelationshipType[];
            var types = results[1];
            var toRemove = this.isSubject ? relations.map((m) => m.Object["AssetTypeUid"].toLowerCase()) : relations.map((m) => m.Subject["AssetTypeUid"].toLowerCase());

            this.items = [...[]];
            types.forEach((ref) => {
                if (!toRemove.some((s) => s === ref.uid.toLowerCase())) {
                    this.items.push({
                        "Text": ref.Name,
                        "Value": ref.uid
                    });
                }
            });
            this.lazyLoadTotalCount = this.items.length;

            this.isLoading = false;
            this.ref.markForCheck();
        });

    }

    loadAssetsLazy($event: LazyLoadEvent) {
        var params = {};
        params["_pageSize"] = $event.rows;
        params["_pageNum"] = ($event.first / $event.rows) + 1;
        params["_order"] = "Name";
        params["_direction"] = "asc";
        params["_includeFields"] = "Name";

        var targetClass = this.isSubject ? this.relationshipType.Object.Class : this.relationshipType.Subject.Class;

        if (targetClass === "Reference") {
            delete params["_includeFields"];
            params["_order"] = "Code";
        }

        let filters: string[] = [];
        if ($event.globalFilter) {
            var value = ($event.globalFilter as string).replace(/'/g, "&apos;");
            value = `${encodeURIComponent(value)}`;
            filters.push(`[Path] ct '${value}'`);
        }

        filters.push(`($Related:${this.intersectTypeUid} ne ${this.assetUid})`);

        if (this.objectCardinality.toString() === "1") {
            filters.push(`($Related:${this.intersectTypeUid} eq null)`);
        }
        params["_filter"] = `(${(filters.join(" and "))}) and (uid ne '${this.assetUid}')`;

        if (this.lazyLoadTotalCount) {
            params["_includeTotal"] = false;
        }

        this.isLoading = true;

        if (this.searchAssetSub) {
            this.searchAssetSub.unsubscribe();
        }

        this.searchAssetSub = this.assetService.getAssets(this.targetAssetTypeUid, params, true).subscribe((res) => {
            if (res.total) {
                this.lazyLoadTotalCount = +res.total;
            }
            this.items = [...[]];
            (res.items as any[]).forEach((item) => {
                var path = item["Path"] as string;
                path = (path as string).replace(/].\[/g, " > ").replace("[", "").replace("]", "");

                this.items.push({
                    "Text": path,
                    "Value": item["AssetUid"]
                });
            });
            this.isLoading = false;
            this.ref.markForCheck();
        });
    }

    private getObjectTypeForTooltip(item: any): string {
        if (item.Value.indexOf('|') == -1) return item.ObjectType;

        return item.Value.split('|')[0];
    }
    private getObjectIdForTooltip(item: any): number {
        if (item.Value.indexOf('|') == -1) return item.Value;

        return item.Value.split('|')[1];
    }

    private handleItemSelection(event) {
        if (this.multiple) {
            this.selectedItems = event;
            var items = [];
            for (let item of event) {
                items.push(item.Value);
            }
            this.value = _.cloneDeep(items);
            this.onModelChange(this.value);
        }
        else {
            if (event) {
                var items = [];
                items.push(event.Value);
                var sel = [];
                sel.push(event);
                this.selectedItems = sel;
                this.value = _.cloneDeep(items);
                this.onModelChange(this.value);
                this.selectedRelationRowIndex = this.items.findIndex((i) => (i.Value === this.value[0]));
            }
        }
    }

    writeValue(value: any): void {
        this.value = value;
    }

    registerOnChange(fn: Function): void {
        this.onModelChange = fn;
    }

    registerOnTouched(fn: Function): void {
        this.onModelTouched = fn;
    }
}