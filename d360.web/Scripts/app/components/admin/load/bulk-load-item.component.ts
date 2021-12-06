import { Input, Output, Component, OnChanges, SimpleChange, EventEmitter } from '@angular/core';
import { LoadService } from '../../../services/load.service';
import { GridColumn } from '../../../models/grid-definition.model';
import { BaseComponent } from '../../shared/base.component'
import { CompanySettingsService } from '../../../services/settings.service';
import { V2ApiFilters } from '../../../models/asset-search.model';

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
    rowsPerPage: number = 10;
    totalRecords: number = 0;
    simpleTextFilter: string;

    get globalFilterFields(): string[] {
        let f = this.columns.map(c => c.datafield);

        f.concat(['Status', 'RowIndex', 'StatusMessage']);

        return f;
    }

    constructor(
        private loadService: LoadService,
        protected settingsService: CompanySettingsService) {
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
        if (this.id == null)
            return;

        this.isLoading = true;

        this.loadService.getLoadColumns(this.id).subscribe(
            data => {
                this.columns = data;

                this.loadService.getLoadUid(this.id).subscribe(r => {
                    console.log(this.simpleTextFilter);
                    this.loadService.getLoadItemsV2(r, this.getParams()).subscribe((data) => {
                        this.items = data.items;
                        this.totalRecords = data.total;

                        this.isLoading = false;
                    })
                }
                );

                
            }
        );
    }

    getData(): void {
        if (this.id == null)
            return;

        this.isLoading = true;

        this.loadService.getLoadUid(this.id).subscribe(r => {
            this.loadService.getLoadItemsV2(r, this.getParams()).subscribe((data) => {
                this.items = data.items;
                this.totalRecords = data.total;

                this.isLoading = false;
            })
        }
        );
    }
    

    getParams() {
        var params = new V2ApiFilters();
        //params._pageNum = this.stateService.artifactTypeFilters.currentPageNumber + 1;

        params._pageSize = this.rowsPerPage;
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
