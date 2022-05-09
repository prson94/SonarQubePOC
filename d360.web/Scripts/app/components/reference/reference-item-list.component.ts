import { Input, Component, OnDestroy, OnChanges, SimpleChanges, ChangeDetectorRef, ViewChild, OnInit } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { AssetService } from '../../services/asset.service';
import { GridDefinitionService } from '../../services/grid-definition.service';
import { GridColumn, GridField } from '../../models/grid-definition.model';
import { Subject, Subscription } from 'rxjs';
import { AdvancedFiltersHelper } from '../../static/advanced-filter-helpers';
import { CompanySettingsService } from '../../services/settings.service';
import { Table } from 'primeng/table';
import { NumberOfRowsByCategories, NumberOfRowsByCategoryService } from '../../services/number-of-rows-by-category.service';
import { takeUntil } from 'rxjs/operators';

@Component({
    selector: 'd3s-reference-item-list',
    templateUrl: './reference-item-list.component.html',
    providers: [AssetService, GridDefinitionService]
})

export class ReferenceItemGridComponent extends BaseComponent implements OnInit, OnChanges, OnDestroy {

    constructor(
        public numberOfRowsByCategoryService: NumberOfRowsByCategoryService,
        private assetService: AssetService,
        private gridDefinitionService: GridDefinitionService,
        protected settingsService: CompanySettingsService,
        private cdRef: ChangeDetectorRef
    ) {
        super(settingsService);
    }

    @Input() assetTypeUid: string;
    @Input() typeName: string;
    @Input() hasAdd: boolean = false;
    @Input() readOnly: boolean = false;
    @Input() isForAssetDetailPage: boolean = false;
    @Input() highlightUid: string = '';

    public rowsPerPage: NumberOfRowsByCategories;
    private sortField: string = 'Code';
    private items: any[] = [];
    private totalRecords: number = 10000;
    private destroy = new Subject<void>();

    columns: GridColumn[] = [];
    fields: GridField[] = [];

    private selected: any;
    showEditor: boolean = false;
    showDelete: boolean = false;
    private getAssetSub: Subscription;
    @ViewChild('dt', { static: false }) table: Table;

    private loadParams = { _loadPermissionDetails: true, _includeParent: true, _order: 'Code', _direction: 'ASC', _pageSize: 10, _pageNum: 1, useGraphForParent: true, _listColorsAsJSON: true };


    add() {
        this.selected = null;
        this.showEditor = true;
    }

    exportMessage: string = '';
    private title: string = 'Items';

    ngOnInit() {
        this.exportMessage = $localize`Export not available for over ${this.maxExportRows} rows`;
        this.setRowsPerPage();
        this.numberOfRowsByCategoryService.defineNumberOfRows(this.defaultInitialItemsPerPage);
    }

    setRowsPerPage(): void {
        this.numberOfRowsByCategoryService.rowsPerPage.pipe(
            takeUntil(this.destroy)
        ).subscribe((rowsPerPage) => {
            this.rowsPerPage = rowsPerPage[this.title];
        });
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes.assetTypeUid && changes.assetTypeUid.currentValue != changes.assetTypeUid.previousValue) {
            this.load();
            this.loadParams._order = 'Code';
            this.loadParams._direction = 'ASC';
            this.loadParams._pageNum = 1;
            this.loadParams._pageSize = 10;
            this.loadParams.useGraphForParent = true;
            this.loadParams._listColorsAsJSON = true;
            delete this.loadParams['_simpleFilter'];
            delete this.loadParams['_filter'];
        }

        if (changes.highlightUid && changes.highlightUid.currentValue !== changes.highlightUid.previousValue && this.highlightUid) {
            var highlightedAsset = this.items.filter((a) => (a.AssetUid as string).toLowerCase() === this.highlightUid.toLowerCase());
            if (highlightedAsset && highlightedAsset[0]) {
                this.selected = highlightedAsset[0];
            }
            else {
                this.load();
            }
        }
    }
    ngOnDestroy() {
        this.getAssetSub.unsubscribe();
        this.destroy.next();
        this.destroy.complete();
    }

    private load() {
        if (!this.assetTypeUid)
            return;

        this.isLoading = true;

        this.gridDefinitionService.getGridDefinition(this.assetTypeUid, 'ReferenceItemType').subscribe(
            result => {
                this.columns = result.Columns;
                this.fields = result.Fields;
                this.loadItems();
            }
        );
    }

    private assetTimeout: any;
    private loadItems() {
        if (this.getAssetSub) {
            this.getAssetSub.unsubscribe();
        }

        this.loadParams.useGraphForParent = false;

        if (this.highlightUid) {
            this.loadParams["_pageWithAsset"] = this.highlightUid;
        }

        this.isLoading = true;
        this.getAssetSub = this.assetService.getAssets(this.assetTypeUid, this.loadParams).subscribe(result => {
            this.items = result.items;
            this.totalRecords = result.total;

            if (this.items.length > 0) {
                this.selected = this.items[0];
            }

            if (this.highlightUid) {
                var highlighted = this.items.filter((a) => (a.AssetUid as string).toLowerCase() === this.highlightUid.toLowerCase());
                if (highlighted) {
                    this.selected = highlighted[0];
                }

                setTimeout(() => {
                    if (this.table) {
                        this.table.first = (+result.pageSize) * (+result.pageNum - 1);
                        this.highlightUid = null;
                        delete this.loadParams["_pageWithAsset"];
                        this.cdRef.markForCheck();
                    }
                }, 100);
            }

            if (this.totalRecords < 1000) {
                this.loadParams.useGraphForParent = false;
            }
            this.isLoading = false;
            this.cdRef.detectChanges();
        },
            err => {
                this.items = [];
                this.totalRecords = 0;
                this.isLoading = false;
                this.cdRef.detectChanges();
            });

    }

    private loadAssets(event) {
        if (event) {
            let sort = event.sortField;
            var field = this.fields.filter(x => x.name.toLowerCase() == event.sortField.toLowerCase())[0];
            if (field)
                sort = field.apiName;

            if (event.sortField == 'Color')
                sort = 'Color';

            if (event.globalFilter && event.globalFilter.length > 0) {
                this.loadParams['_simpleFilter'] = event.globalFilter;
            }
            else {
                delete this.loadParams['_simpleFilter'];
            }

            var advancedFilter = AdvancedFiltersHelper.parseFiltersFromTableFilters(event.filters, this.fields);
            if (advancedFilter.length > 0) {
                this.loadParams['_filter'] = advancedFilter;
            }
            else {
                delete this.loadParams['_filter'];
            }

            this.loadParams._order = sort ? sort : this.sortField;
            this.loadParams._direction = event.sortOrder == 1 ? 'ASC' : 'DESC';

            this.loadParams._pageSize = +event.rows;
            this.loadParams._pageNum = (+event.first / +event.rows) + 1;
        }

        this.loadItems();
    }



    private export() {
        this.assetService.downloadAssetsExcel(this.assetTypeUid, this.loadParams, this.typeName);
    }

    public onDeleted() {
        this.items = this.items.filter(x => x.AssetUid != this.selected.AssetUid);
        this.selected = null;
        this.showDelete = false;
    }

    public saveReferenceItem(event) {
        this.showEditor = false;
    }
    private canExportRecords() {
        return this.totalRecords <= this.maxExportRows;
    }
}
