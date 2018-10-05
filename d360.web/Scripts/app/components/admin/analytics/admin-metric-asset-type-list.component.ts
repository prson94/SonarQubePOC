import { Input, Component, EventEmitter, Output, OnInit, OnChanges, SimpleChange } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { MetricsService } from '../../../services/metrics.service';
import { Item } from '../../../models/metrics.model';
import { FormMode } from '../../../models/form.model';
import { MessagesService } from '../../../services/messages.service';
import { AssetTypeMetricModel } from '../../../models/asset.model';

@Component({
    selector: 'd3s-admin-metric-asset-type-list',
    template: `
<header>Asset Types</header>
<d3s-loading [isLoading]="isLoading"></d3s-loading>
<div *ngIf="!isLoading">
    <input #gb [hidden]="!showSimpleFilter" type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
    <p-dataTable #dt [value]="models" selectionMode="single" [globalFilter]="gb" [(selection)]="selection" (onRowSelect)="onRowSelect($event)" [rows]="10" [paginator]="true" [pageLinks]="3">
        <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>
        <p-column field="Class" header="Class" [sortable]="true" [filter]="!showSimpleFilter" [style]="{width:'70px'}"></p-column>
        <p-column field="Name" header="Name" [sortable]="true" [filter]="!showSimpleFilter"></p-column>
    </p-dataTable>
</div>
                `,
    providers: [MetricsService]
})

export class AdminMetricAssetTypeListComponent extends BaseComponent implements OnInit, OnChanges {
    @Output() selectionChange = new EventEmitter();

    private models: AssetTypeMetricModel[] = [];
    private selection: AssetTypeMetricModel = null;

    constructor(private metricsService: MetricsService, protected messagesService: MessagesService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    ngOnChanges() {
        this.load();
    }

    load(): Promise<any> {
        this.isLoading = true;
        return this.metricsService.getAssetTypes()
            .then(r => {
                this.models = r;
                this.isLoading = false;
                if (this.models.length && this.models.length > 0) {
                    this.selection = this.models[0];
                    this.selectionChange.emit(this.selection);
                }
            });
    }

    onRowSelect(e: any) {
        this.selection = e.data;
        this.selectionChange.emit(this.selection);
    }
};