import { Input, Component, EventEmitter, Output, OnInit, OnChanges, SimpleChange } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { MetricsService } from '../../../services/metrics.service';
import { Item } from '../../../models/metrics.model';
import { FormMode } from '../../../models/form.model';
import { AssetTypeMetricModel } from '../../../models/asset.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';

@Component({
    selector: 'd3s-admin-metric-asset-type-list',
    template: `
<header>Asset Types</header>
<d3s-loading [isLoading]="isLoading"></d3s-loading>
<div *ngIf="!isLoading">

    <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..." class="grid-simple-filter">
    <p-table #dt [value]="models" selectionMode="single" [globalFilterFields]="['ClassName','Name']" [pageLinks]="3" [paginator]="true" [rows]="10" [(selection)]="selection" (onRowSelect)="onRowSelect($event)" >
        <ng-template pTemplate="header">
            <tr>
                <th [pSortableColumn]="'ClassName'" style="width: 150px">
                    Class
                    <d3s-sortIcon [field]="'Class'"></d3s-sortIcon>
                </th>
                <th [pSortableColumn]="'Name'">
                    Name
                    <d3s-sortIcon [field]="'Name'"></d3s-sortIcon>
                </th>
            </tr>
            <tr [hidden]="showSimpleFilter">
                <th><d3s-column-filter [field]="'ClassName'" [datatype]="'text'"></d3s-column-filter></th>
                <th><d3s-column-filter [field]="'Name'" [datatype]="'text'"></d3s-column-filter></th>
            </tr>
        </ng-template>
        <ng-template pTemplate="body" let-item>
            <tr [pSelectableRow]="item">
                <td>{{item.ClassName}}</td>
                <td>{{item.Name}}</td>
            </tr>
        </ng-template>
        <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
            <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
        </ng-template>
    </p-table>
</div>
                `,
    providers: [MetricsService]
})

export class AdminMetricAssetTypeListComponent extends BaseComponent implements OnInit, OnChanges {
    @Output() selectionChange = new EventEmitter();

    private models: AssetTypeMetricModel[] = [];
    private selection: AssetTypeMetricModel = null;

    constructor(private metricsService: MetricsService, protected messagesService: MessagesObservableService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    ngOnChanges() {
        this.load();
    }

    load() {
        this.isLoading = true;
        this.metricsService.getAssetTypes()
            .subscribe(r => {
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