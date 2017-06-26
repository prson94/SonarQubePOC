import { Input, Output, Component, OnChanges, SimpleChange, EventEmitter } from '@angular/core';
import { LoadDetail } from '../../../models/load.model';
import { LoadService } from '../../../services/load.service';
import { GridColumn } from '../../../models/grid-definition.model';
import { BaseComponent } from '../../shared/base.component'

@Component({
    selector: 'd3s-bulk-load-item',
    template: `
            <d3s-loading [isLoading]="isLoading"></d3s-loading>
            <div *ngIf="!isLoading">
                <header>
                    {{title}}
                    <d3s-tile-actions [hasFilterMode]="true" [(filterMode)]="showSimpleFilter" [hasRefresh]="true" (refreshClick)="refresh()" [hasExportErrors]="true" (exportErrorsClick)="exportErrors()" [hasExportOriginal]="true" (exportOriginalClick)="exportOriginal()"></d3s-tile-actions>                                    
                </header>
                <input #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
                <p-dataTable #dt [globalFilter]="gb" [value]="items" selectionMode="single" [rows]="25" paginator="true" scrollable="true" scrollWidth="100%" [rowsPerPageOptions]="defaultPagingOptions">
                    <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                    <p-column field="Status" header="Status" sortable="true" [style]="{'width':'125px'}" [filter]="!showSimpleFilter"></p-column>
                    <p-column *ngFor="let column of columns" [field]="column.datafield" [header]="column.text" [style]="{'width':'250px'}" [filter]="!showSimpleFilter"></p-column>
                    <p-column field="RowIndex" header="Row" sortable="true" [style]="{'width':'100px'}" [filter]="!showSimpleFilter"></p-column>        
                    <p-column field="StatusMessage" header="Message" sortable="true" [style]="{'width':'250px'}" [filter]="!showSimpleFilter">
                        <ng-template let-item="rowData" pTemplate type="body">
                            <span [innerHtml]="item.StatusMessage"></span>
                        </ng-template>
                    </p-column>
                </p-dataTable>
            </div>
    `,
    providers: [LoadService]
})

export class BulkLoadItemComponent extends BaseComponent implements OnChanges {
    @Input() id: number;
    @Input() title: string = "Load Details";

    @Output() refreshClick = new EventEmitter();
    
    columns: GridColumn[];
    items: any[];


    constructor(private loadService: LoadService) {
        super();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p == 'id') {
                this.load();                
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

        this.loadService.getLoadColumns(this.id)
            .then(data => {
                this.columns = data;
            })
            .then(() => this.loadService.getLoadItems(this.id))
            .then(data => {
                this.items = data;
                this.isLoading = false;
            });
    }

    refresh(): void {
        this.load();
        this.refreshClick.emit();
    }
}
