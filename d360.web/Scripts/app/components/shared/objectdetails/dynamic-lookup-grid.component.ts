import { Component, Input, OnInit, ChangeDetectorRef, ChangeDetectionStrategy, OnDestroy } from "@angular/core";
import { LookupGrid, GridFilterColumn } from "../../../models/grid-definition.model";
import { Router } from "@angular/router";
import { SiteUrlHelpers } from "../../../static/site-url-helpers";
import { BaseComponent } from "../base.component";
import { DetailField } from "../../../models/object-detail.model";
import { AssetService } from "../../../services/asset.service";
import { Subscription } from "rxjs";
import { CompanySettingsService } from "../../../services/settings.service";

@Component({
    selector: "d3s-dynamic-lookup-grid",
    templateUrl: "./dynamic-lookup-grid.component.html",
    providers: [AssetService],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class DynamicLookupGridComponent extends BaseComponent implements OnDestroy {
    @Input() data: LookupGrid;
    @Input() field: DetailField;
    @Input() hideFooter = false;
    @Input() hideHeader = false;
    @Input() hideFilter = true;
    @Input() assetUid: string = '';

    isComplex = false;
    showSimpleFilter = true;
    isColumnsLoaded = false;

    visibleColumns: GridFilterColumn[] = [];
    private loadSubscription: Subscription;
    private currentFilters: any;

    get globalFilterFields(): string[] {
        return this.visibleColumns.map(c => c.datafield);
    }

    constructor(
        private assetService: AssetService,
        protected settingsService: CompanySettingsService,
        private cdRef: ChangeDetectorRef,
        private router: Router
    ) {
        super(settingsService);
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

        this.isComplex = (this.data.Fields.find(f => f.name == 'Url') == null);

        //do this on init to avoid binding to function call
        this.data.Columns.forEach(c => {
            c.type = this.columnDataType(c);
            if (c.type == 'number') {
                this.data.Values.forEach(v => {
                    v[c.datafield] = this.formatAsNumber(v[c.datafield]);
                });
            }
            if (c.type == 'string' || c.type == 'preview' || c.type == 'lookup' || c.type == 'html') {
                this.data.Values.forEach(v => {
                    if (v[c.datafield] === null) {
                        v[c.datafield] = ''; //prevent IE from displaying 'null'
                    }
                });
            }
        });

        this.data.Columns.filter(c => c.type == 'hidden').forEach(c => {
            let i = this.data.Columns.find(i => i.datafield == c.text);
            if (i) {
                i.type = 'preview';
            }
        });

        this.visibleColumns = this.data.Columns.filter(c => c.type != 'hidden');

        this.isColumnsLoaded = true;

    }

    private formatAsNumber(val): string {
        return val !== '' && val !== null ? Number(val).toLocaleString() : "";
    }

    private columnDataType(column: GridFilterColumn): string {
        var fields = this.data.Fields.filter(x => x.name == column.datafield);

        if (column.type == 'preview')
            return 'preview';
        if ((column.datafield == 'Name' || column.datafield == 'TextPath') && !this.isComplex)
            return 'tooltip';
        if (fields.length > 0)
            return fields[0].type;
        return 'string';
    }

    navigate(url: string) {
        this.router.navigateByUrl(SiteUrlHelpers.convertClassicUrl(url));
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
            if (event.sortOrder == 1) {
                params['_direction'] = 'ASC';
            }
            else {
                params['_direction'] = 'DESC';
            }
        }

        if (event.filters) {
            if (event.filters['global']) {
                params['simpleFilter'] = event.filters.global.value;
            }

            var keys = Object.keys(event.filters).filter(x => x != 'global');
            var advFilters: string[] = [];

            keys.forEach(key => {
                var q = key + ' ct ' + `'${encodeURIComponent(event.filters[key].value)}'`;
                advFilters.push(q);
            });

            if (advFilters.length > 0) {
                delete params['simpleFilter'];
                params['filter'] = advFilters.join(" and ");
            }
        }


        if (this.loadSubscription) {
            this.loadSubscription.unsubscribe();
        }

        this.currentFilters = params;

        this.loadSubscription = this.assetService.getAssetsComplexFieldValue(this.assetUid, this.field.FieldName, params)
            .subscribe(result => {
                this.data = result;
                this.loadInitialInfo();
                this.isLoading = false;
                this.cdRef.markForCheck();
            }, null, () => {
                this.isLoading = false;
                this.cdRef.markForCheck();
            });
    }

    getScoreFieldHTML(data: any, colName: string): string {
        var value = data[colName] as string;
        if (!value) return '';

        let cleanValue: number = parseFloat(value.replace("%", ""));
        let fieldTypeID: number = parseInt(colName.split("_")[1]);
        var className = "";
        if (this.data?.ScoringInfo) {
            className = "score-poor";
            var allocInfo = this.data?.ScoringInfo?.filter(x => x["FieldTypeId"] == fieldTypeID);
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
}
