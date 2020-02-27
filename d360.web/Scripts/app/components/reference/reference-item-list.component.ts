import { Input, Component, EventEmitter, Output, OnInit, OnDestroy, ChangeDetectionStrategy, OnChanges, SimpleChanges } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { AssetService } from '../../services/asset.service';
import { ReferenceItemType } from '../../models/reference.model';
import { GridDefinitionService } from '../../services/grid-definition.service';
import { GridColumn, GridField } from '../../models/grid-definition.model';
import { debounceTime } from 'rxjs/operators';

@Component({
    selector: 'd3s-reference-item-list',
    templateUrl: './reference-item-list.component.html',
    providers: [AssetService, GridDefinitionService]
})

export class ReferenceItemGridComponent extends BaseComponent implements OnInit, OnChanges {

    constructor(
        private assetService: AssetService,
        private gridDefinitionService: GridDefinitionService
    ) {
        super();
    }

    @Input() assetTypeUid: string;
    private sortField: string = 'Code';
    private items: any[] = [];
    private totalRecords: number = 10000;

    columns: GridColumn[] = [];
    fields: GridField[] = [];

    private selected: any;
    private showEditor: boolean = false;
    private showDelete: boolean = false;

    private loadParams = { _loadPermissionDetails: true, _order: 'Code', _direction: 'ASC', _pageSize: 10, _pageNum: 1 };


    add() {
        this.selected = null;
        this.showEditor = true;
    }

    private title: string = 'Items';

    ngOnChanges(changes: SimpleChanges) {
        if (changes.assetTypeUid.currentValue != changes.assetTypeUid.previousValue) {
            this.load();
        }
    }

    ngOnInit() {

    }

    private load() {
        console.log("loading");
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

    private loadItems() {

        this.assetService.getAssets(this.assetTypeUid, this.loadParams).subscribe(result => {
            console.log(result);
            this.items = result.items;
            this.totalRecords = result.total;
            if (this.items.length > 0) {
                this.selected = this.items[0];
            }
            this.isLoading = false;
        });
    }

    private loadAssets(event) {
        console.log(event);
        if (event) {
            this.loadParams._order = event.sortField;
            this.loadParams._direction = event.sortOrder == 1 ? 'ASC' : 'DESC';

            this.loadParams._pageSize = +event.rows;
            this.loadParams._pageNum = (+event.first / +event.rows) + 1;
        }

        this.loadItems();
    }

    private export() {
        console.log("exporting");
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
