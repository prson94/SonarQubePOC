import { Input, Component, OnDestroy, OnChanges, SimpleChanges, ChangeDetectorRef } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { AssetService } from '../../services/asset.service';
import { GridDefinitionService } from '../../services/grid-definition.service';
import { GridColumn, GridField } from '../../models/grid-definition.model';
import { Subscription } from 'rxjs';
import { AdvancedFiltersHelper } from '../../static/advanced-filter-helpers';
import { CompanySettingsService } from '../../services/settings.service';

@Component({
    selector: 'd3s-reference-item-list',
    templateUrl: './reference-item-list.component.html',
    providers: [AssetService, GridDefinitionService]
})

export class ReferenceItemGridComponent extends BaseComponent implements OnChanges, OnDestroy {

    constructor(
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

    private sortField: string = 'Code';
    private items: any[] = [];
    private totalRecords: number = 10000;

    columns: GridColumn[] = [];
    fields: GridField[] = [];

    private selected: any;
    showEditor: boolean = false;
    showDelete: boolean = false;
    private getAssetSub: Subscription;


    private loadParams = { _loadPermissionDetails: true, _includeParent: true, _order: 'Code', _direction: 'ASC', _pageSize: 10, _pageNum: 1, useGraphForParent: true, _listColorsAsJSON: true };


    add() {
        this.selected = null;
        this.showEditor = true;
    }

    private title: string = 'Items';

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
    }
    ngOnDestroy() {
        this.getAssetSub.unsubscribe();
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
        this.isLoading = true;
        this.getAssetSub = this.assetService.getAssets(this.assetTypeUid, this.loadParams).subscribe(result => {
            this.items = result.items;
            this.totalRecords = result.total;

            if (this.items.length > 0) {
                this.selected = this.items[0];
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
