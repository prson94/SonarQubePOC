import { Component, Input, OnInit, ChangeDetectorRef, ChangeDetectionStrategy, OnDestroy } from "@angular/core";
import { LookupGrid, GridFilterColumn, LookupGridField } from "../../../models/grid-definition.model";
import { NavigationEnd, Router } from "@angular/router";
import { SiteUrlHelpers } from "../../../static/site-url-helpers";
import { BaseComponent } from "../base.component";
import { ComplexLookupType, DetailField } from "../../../models/object-detail.model";
import { AssetService } from "../../../services/asset.service";
import { Subscription } from "rxjs";
import { Filters } from "../../assets-grid/advanced-filtering/advanced-filtering.models";

declare var CurrentResourceID;

@Component({
    selector: "ig-asset-lookup-grid",
    templateUrl: "./asset-lookup-grid.component.html",
    styleUrls: ["asset-lookup-grid.component.less"],
    providers: [AssetService],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class AssetLookupGridComponent extends BaseComponent implements OnDestroy {
    @Input() data: LookupGrid;
    @Input() field: DetailField;
    @Input() hideFooter = false;
    @Input() hideHeader = false;
    @Input() hideFilter = true;
    @Input() assetUid: string = '';

    isReferenceListFromRelationship = false;
    showDescription = false;
    lookupField: LookupGridField;

    isComplex = false;
    showSimpleFilter = true;
    isColumnsLoaded = false;

    visibleColumns: GridFilterColumn[] = [];
    loadSubscription: Subscription;
    currentFilters: any;

    showAdvancedFilterField: boolean = true;

    simpleSearchTooltipHTML: string = `<p>Type to provide a search term. Matches will be found where the value of any column starts with the term or terms provided.</p><p>You can also use wildcards for more control over how the term is matched.
*account* : Match on values which contain 'account'</p><p>All matches are case insensitive.</p>`;

    simpleTextFilter: string;

    get globalFilterFields(): string[] {
        return this.visibleColumns.map((c) => c.datafield);
    }

    constructor(private router: Router,
        private assetService: AssetService,
        private cdRef: ChangeDetectorRef
    ) {
        super();
        this.showAdvancedFilterField = !this.router.url.startsWith("/sidebar/visualization/");
        this.areFiltersLoaded = true;
    }

    ngOnDestroy() {
        if (this.loadSubscription) {
            this.loadSubscription.unsubscribe();
        }
    }

    private loadInitialInfo(): void {
        if (this.isColumnsLoaded) {
            return;
        }

        this.isReferenceListFromRelationship = !!(this.data as any).isReferenceListFromRelationship;
        this.isComplex = (this.data.Fields.find((f) => f.name === 'Url') == null);

        if (this.isReferenceListFromRelationship) {
            this.lookupField = (this.data as any);

            let show = localStorage.getItem(`lookup_description_${CurrentResourceID}_${this.lookupField.fieldTypeId}`);
            if (show == null) {
                this.showDescription = this.lookupField.showDescription;
            } else {
                this.showDescription = show === "true" ? true : false;
            }
        }

        //do this on init to avoid binding to function call
        this.data.Columns.forEach((c) => {
            c.type = this.columnDataType(c);
            if (c.type === 'number') {
                this.data.Values.forEach((v) => {
                    v[c.datafield] = this.formatAsNumber(v[c.datafield]);
                });
            }
            if (c.type === 'string' || c.type === 'preview' || c.type === 'lookup' || c.type === 'html') {
                this.data.Values.forEach((v) => {
                    if (v[c.datafield] === null) {
                        v[c.datafield] = ''; //prevent IE from displaying 'null'
                    }
                });
            }
        });

        this.data.Columns.filter((c) => c.type === 'hidden')
            .forEach((c) => {
                let i = this.data.Columns.find((i) => i.datafield === c.text);
                if (i) {
                    i.type = 'preview';
                }
            });

        this.visibleColumns = this.data.Columns.filter((c) => c.type !== 'hidden');

        this.isColumnsLoaded = true;
    }

    private formatAsNumber(val): string {
        return val !== '' && val !== null ? Number(val).toLocaleString() : "";
    }

    private columnDataType(column: GridFilterColumn): string {
        var fields = this.data.Fields.filter((x) => x.name === column.datafield);

        if (column.type === 'preview') {
            return 'preview';
        }
        if ((column.datafield === 'Name' || column.datafield === 'TextPath') && !this.isComplex) {
            return 'tooltip';
        }
        if (fields.length > 0) {
            return fields[0].type;
        }
        return 'string';
    }

    navigate(url: string, e: any) {
        this.router.navigateByUrl(SiteUrlHelpers.convertClassicUrl(url));
        if (e) {
            e.preventDefault();
        }
    }

    export() {
        var params = this.currentFilters;
        params['_pageSize'] = 10000;
        params['_pageNum'] = 1;
        let fileName: string = this.field.FieldName;
        if (this.data['name']) {
            fileName = this.data['name'];
        }
        this.assetService.getAssetsComplexFieldValue(this.assetUid, this.field.FieldName, params, true, fileName);
    }

    loadData(event) {
        this.eventData = event;

        if (!this.areFiltersLoaded) {
            return;
        }
        this.isLoading = true;
        var params = {};
        if (event.rows) {
            params['_pageSize'] = event.rows;
        }
        if (event.first) {
            params['_pageNum'] = Math.round(event.first / event.rows) + 1;
        }
        if (event.sortField) {
            params['_order'] = event.sortField;
        }

        params['useUidUrls'] = 'false';

        if (event.sortOrder) {
            if (event.sortOrder === 1) {
                params['_direction'] = 'ASC';
            }
            else {
                params['_direction'] = 'DESC';
            }
        }

        if (this.simpleTextFilter) {
            params['simpleFilter'] = this.simpleTextFilter;
        }
        else {
            delete params['simpleFilter'];
        }

        if (this.newAdvancedFilters && this.newAdvancedFilters.filter) {
            params['filter'] = this.newAdvancedFilters.filter;
        }
        else {
            delete params['filter'];
        }

        if (this.loadSubscription) {
            this.loadSubscription.unsubscribe();
        }

        this.currentFilters = params;

        this.loadSubscription = this.assetService.getAssetsComplexFieldValue(this.assetUid, this.field.FieldName, params)
            .subscribe((result) => {
                if (result) {
                    this.data = result;
                    this.loadInitialInfo();
                }
                this.isLoading = false;
                this.cdRef.markForCheck();
            }, () => {
                this.isLoading = false;
                this.cdRef.markForCheck();
            }, () => {
                this.isLoading = false;
                this.cdRef.markForCheck();
            });
    }

    getScoreFieldHTML(data: any, colName: string): string {
        var value = data[colName] as string;
        if (!value) {
            return '';
        }

        let cleanValue: number = parseFloat(value.replace("%", ""));
        let fieldTypeID: number = parseInt(colName.split("_")[1]);
        var className = "";
        if (this.data?.ScoringInfo) {
            className = "score-poor";
            var allocInfo = this.data?.ScoringInfo?.filter((x) => x["FieldTypeId"] === fieldTypeID);
            if (allocInfo.length > 0) {
                var alloc = allocInfo[0];
                if (cleanValue > parseFloat(alloc.LowerThreshold)) {
                    className = "score-average";
                }
                if (cleanValue > parseFloat(alloc.UpperThreshold)) {
                    className = "score-good";
                }
            }
        }

        return `<div class="score-pill-small ${className}"></div><span>${value}</span>`;
    }

    toggleShowDescription() {
        this.showDescription = !this.showDescription;
        localStorage.setItem(`lookup_description_${CurrentResourceID}_${this.lookupField.fieldTypeId}`, this.showDescription.toString());
    }

    onFiltersLoaded() {
        this.areFiltersLoaded = true;
        this.loadData(this.eventData);
    }

    areFiltersLoaded: boolean = false;
    newAdvancedFilters: Filters;
    eventData: any;
    public advancedFiltersChanged($event) {
        this.newAdvancedFilters = $event;
        this.loadData(this.eventData);
    }

    get filtersLoadIdentifier() {
        return "ComplexField" + this.assetUid + "|" + this.field.FieldName + "|" + this.field.DataType;
    }
}
