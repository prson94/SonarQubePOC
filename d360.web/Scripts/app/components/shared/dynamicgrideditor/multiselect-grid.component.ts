import { Input, Component, Output, EventEmitter, OnInit, forwardRef, ChangeDetectionStrategy, ChangeDetectorRef, OnChanges } from '@angular/core';
import { BaseComponent } from '../base.component';
import { UriBasedService } from '../../../services/uri-based.service';
import * as _ from 'lodash';
import { NG_VALUE_ACCESSOR, ControlValueAccessor } from '@angular/forms';
import { LazyLoadEvent } from 'primeng/api';
import { AssetService } from '../../../services/asset.service';
import { Subscription } from 'rxjs';

export const MULTISELECT_GRID_VALUE_ACCESSOR: any = {
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => MultiSelectGridComponent),
    multi: true
};

@Component({
    selector: 'd3s-multiselect-grid',
    templateUrl: "multiselect-grid.component.html",
    providers: [MULTISELECT_GRID_VALUE_ACCESSOR, AssetService],
    changeDetection: ChangeDetectionStrategy.OnPush,
})

export class MultiSelectGridComponent extends BaseComponent implements ControlValueAccessor {
    @Input() multiple: boolean = true;
    @Input() intersectTypeUid: string;
    @Input() assetUid: string;
    @Input() targetAssetTypeUid: string;
    @Input() objectCardinality: string;


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
        private ref: ChangeDetectorRef) {
        super();
    }

    loadAssetsLazy($event: LazyLoadEvent) {

        var params = {};
        params["_pageSize"] = $event.rows;
        params["_pageNum"] = ($event.first / $event.rows) + 1;
        params["_order"] = "Name";
        params["_direction"] = "asc";
        params["_includeFields"] = "Name";

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
        params["_filter"] = `(${(filters.join(" and "))})`;

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
            })
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