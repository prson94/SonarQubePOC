import { Input, Output, Component, OnChanges, SimpleChange, EventEmitter, ChangeDetectorRef } from '@angular/core';
import { LoadService } from '../../../services/load.service';
import { GridColumn } from '../../../models/grid-definition.model';
import { BaseComponent } from '../../shared/base.component'
import { CompanySettingsService } from '../../../services/settings.service';
import { V2ApiFilters } from '../../../models/asset-search.model';
import { LazyLoadEvent } from 'primeng/api';
import { Subscription } from 'rxjs';
import { debounceTime } from 'rxjs/operators';
import { SortOrder } from '../../../models/enums.model';

@Component({
    selector: 'd3s-bulk-load-item',
    templateUrl: './bulk-load-item.component.html',
    providers: [LoadService]
})

export class BulkLoadItemComponent extends BaseComponent implements OnChanges {
    @Input() id: number;
    @Input() title: string = "Load Details";

    @Output() refreshClick = new EventEmitter();

    columns: GridColumn[];
    items: any[];
    rowsPerPage: number = 25;
    totalRecords: number = 0;
    simpleTextFilter: string;
    pageNum: number = 1;
    firstPage: number = 0;
    sortOrder: number = SortOrder.None;
    sortField: string = "";
    itemsLoading: boolean = false;
    private itemsSearchSub: Subscription;

    get globalFilterFields(): string[] {
        let f = this.columns.map(c => c.datafield);

        f.concat(['Status', 'RowIndex', 'StatusMessage']);

        return f;
    }

    constructor(
        private loadService: LoadService,
        protected settingsService: CompanySettingsService,
        private changeDetectorRef: ChangeDetectorRef,    ) {
        super(settingsService);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p == 'id') {
                return this.load();
            }
        }

        this.load();
    }

    ngOnDestroy() {
        if (this.itemsSearchSub) {
            this.itemsSearchSub.unsubscribe();
        }
    }

    exportErrors(): void {
        if (this.id == null)
            return;

        this.loadService.getLoadErrorsXls(this.id);
    }

    exportOriginal(): void {
        if (this.id == null)
            return;

        this.loadService.getLoadOriginalXls(this.id);
    }

    load(): void {
        this.isLoading = true;
        this.loadService.getLoadColumns(this.id).subscribe(
            (columnData) => {
                this.columns = columnData;
                this.isLoading = false;
                this.firstPage = 0;
                this.pageNum = 0;
                this.getData();
            }
        );
    }

    getData(): void {
        if (this.id == null)
            return;
    
        if (this.itemsSearchSub) {
            this.itemsSearchSub.unsubscribe();
        }

        this.itemsLoading = true;

        this.loadService.getLoadUid(this.id).subscribe((r) => {
            this.itemsSearchSub =  this.loadService.getLoadItemsV2(r, this.getParams()).pipe(debounceTime(400)).subscribe((data) => {
                this.items = data.items;
                this.totalRecords = data.total;
                this.itemsLoading = false;
            });
        })
    }


    loadItemsLazy(event: LazyLoadEvent) {
        this.firstPage = event.first;
        this.pageNum = event.first / event.rows;
        this.sortOrder = event.sortOrder;
        this.sortField = event.sortField;
        this.rowsPerPage = event.rows;
        this.getData();
    }
    

    getParams() {
        var params = new V2ApiFilters();
        params._pageNum = this.pageNum + 1;

        params._pageSize = this.rowsPerPage;
        if (this.sortField) {
            params._order = this.sortField;
        }
        else {
            delete params['_order'];
        }

        if (this.sortOrder !== SortOrder.None) {
            params._direction = this.sortOrder === SortOrder.Ascending ? "asc" : "desc";
        }
        else {
            delete params['_direction'];
        }

        if (this.simpleTextFilter && this.simpleTextFilter.length > 0) {
            params._simpleFilter = encodeURIComponent(this.simpleTextFilter);
        }
        else {
            delete params['_simpleFilter'];
        }

        return params;
    }

    public onSimpleSearch($event) {
        this.getData();
    }

    refresh(): void {
        this.load();
        this.refreshClick.emit();
    }
}
