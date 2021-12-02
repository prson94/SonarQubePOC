import { Input, Output, Component, OnChanges, SimpleChange, EventEmitter } from '@angular/core';
import { LoadService } from '../../../services/load.service';
import { GridColumn } from '../../../models/grid-definition.model';
import { BaseComponent } from '../../shared/base.component'
import { CompanySettingsService } from '../../../services/settings.service';

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

                this.loadService.getLoadItems(this.id).subscribe((data) => {
                    this.items = data;

                    this.isLoading = false;
                })
            }
        );
    }

    refresh(): void {
        this.load();
        this.refreshClick.emit();
    }
}
