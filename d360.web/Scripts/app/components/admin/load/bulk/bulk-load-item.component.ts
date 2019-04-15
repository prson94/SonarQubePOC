import {
    Input,
    Output,
    Component,
    OnChanges,
    SimpleChange,
    EventEmitter, OnInit
} from '@angular/core';

import {GridColumn} from "../../../../models/grid-definition.model";

import {BulkLoadItemService} from './bulk-load-item.service';

import {BaseComponent} from '../../../shared/base.component'

@Component({
    selector: 'd3s-bulk-load-item',
    templateUrl: './bulk-load-item.component.html',
    providers: [BulkLoadItemService]
})

export class BulkLoadItemComponent extends BaseComponent implements OnInit, OnChanges {
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

    constructor(private bulkLoadItemService: BulkLoadItemService) {
        super();
    }

    ngOnInit(): void {
        this.load();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p == 'id') {
                return this.load();
            }
        }

        this.load();
    }

    private checkId():boolean {
        return this.id == null;
    }

    exportErrors(): void {
        if (this.checkId()) {
            this.bulkLoadItemService.getLoadErrorsXls(this.id);
        }
    }

    exportOriginal(): void {
        if (this.checkId()) {
            this.bulkLoadItemService.getLoadOriginalXls(this.id);
        }
    }

    load(): void {
        if (this.checkId()) {
            this.isLoading = true;

            this.bulkLoadItemService.getLoadColumns(this.id).subscribe(
                responseLoadColumns => {
                    this.columns = responseLoadColumns;

                    this.bulkLoadItemService.getLoadItems(this.id).subscribe(
                        responseLoadItems => {
                            this.items = responseLoadItems;

                            this.isLoading = false;
                        }
                    )
                }
            );
        }
    }

    refresh(): void {
        this.load();
        this.refreshClick.emit();
    }
}
