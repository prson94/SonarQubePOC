import { Input, Component, EventEmitter, Output, OnInit, OnDestroy, ChangeDetectionStrategy, OnChanges, SimpleChanges, ChangeDetectorRef } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { AssetService } from '../../services/asset.service';
import { ReferenceItemType } from '../../models/reference.model';
import { GridDefinitionService } from '../../services/grid-definition.service';
import { GridColumn, GridField } from '../../models/grid-definition.model';
import { debounceTime } from 'rxjs/operators';
import { Subscription } from 'rxjs';

@Component({
    selector: 'd3s-reference-item-list',
    templateUrl: './reference-item-list.component.html',
    providers: [AssetService, GridDefinitionService]
})

export class ReferenceItemGridComponent extends BaseComponent implements OnInit, OnChanges, OnDestroy {

    constructor(
        private assetService: AssetService,
        private gridDefinitionService: GridDefinitionService,
        private cdRef: ChangeDetectorRef
    ) {
        super();
    }

    @Input() assetTypeUid: string;
    @Input() typeName: string;
    private sortField: string = 'Code';
    private items: any[] = [];
    private totalRecords: number = 10000;

    columns: GridColumn[] = [];
    fields: GridField[] = [];

    private selected: any;
    private showEditor: boolean = false;
    private showDelete: boolean = false;
    private getAssetSub: Subscription;

    private loadParams = { _loadPermissionDetails: true, _includeParent: true, _order: 'Code', _direction: 'ASC', _pageSize: 10, _pageNum: 1, useGraphForParent: true };


    add() {
        this.selected = null;
        this.showEditor = true;
    }

    private title: string = 'Items';

    ngOnChanges(changes: SimpleChanges) {
        if (changes.assetTypeUid.currentValue != changes.assetTypeUid.previousValue) {
            this.load();
            this.loadParams._order = 'Code';
            this.loadParams._direction = 'ASC';
            this.loadParams._pageNum = 1;
            this.loadParams._pageSize = 10;
            this.loadParams.useGraphForParent = true;
            delete this.loadParams['_simpleFilter'];
            delete this.loadParams['_filter'];

        }
    }

    ngOnInit() {

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
        window.clearTimeout(this.assetTimeout);
        if (this.getAssetSub)
            this.getAssetSub.unsubscribe();
        this.assetTimeout = window.setTimeout(() => {
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
            });
        }, 300)

    }

    private loadAssets(event) {
        if (event) {
            let sort = event.sortField;
            var field = this.fields.filter(x => x.name.toLowerCase() == event.sortField.toLowerCase())[0];
            if (field)
                sort = field.apiName;

            if (event.globalFilter && event.globalFilter.length > 0) {
                this.loadParams['_simpleFilter'] = event.globalFilter;
            }
            else {
                delete this.loadParams['_simpleFilter'];
            }

            var advancedFilter = this.getAdvancedFilter(event.filters);
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

    private getAdvancedFilter(data): string {
        var props = Object.keys(data);
        var ret: string = '';
        props.forEach(prop => {
            let fieldName = prop;
            var value = data[prop];
            var field = this.fields.filter(x => x.name.toLowerCase() == prop.toLowerCase())[0];
            if (field)
                fieldName = field.apiName;

            ret += `${fieldName} ct '${value}'`;
            if (prop != props[props.length - 1]) {
                ret += " and ";
            }
        });

        return ret;
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

}
