import { Input, Component, EventEmitter, Output } from '@angular/core';
import { LazyLoadEvent, DataTable } from 'primeng/primeng';
import { BaseComponent } from '../shared/base.component';
import { FusionService } from '../../services/fusion.service';
import { FusionExecutionResult } from '../../models/fusion.model';
import { SortOrder } from '../../models/enums.model';

@Component({
    selector: 'd3s-fusion-execution-results',
    template: `     
                    <header>Execution History - Result Details<d3s-tile-actions [hasExport]="true" (exportClick)="export()"></d3s-tile-actions></header>                    
                    <input type="text" style="width: 100%;" maxlength="200" (keyup)="checkSimpleSearchEnter($event,dt);" [(ngModel)]="simpleTextFilter" placeholder="Search..." autofocus autocomplete="off" />                                                                            
                    <p-dataTable #dt scrollable="true" scrollWidth="100%" [value]="results" selectionMode="single" [rows]="5" [rowsPerPageOptions]="[5,10,20]" [paginator]="true" [pageLinks]="3" [(selection)]="selected" (onLazyLoad)="loadResultsLazy($event)" lazy="true" [totalRecords]="resultCount">                            
                            <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                            <p-column field="FusionAttributeType" header="Type" [sortable]="true" [style]="{width:'100px'}"></p-column>
                            <p-column field="FusionAttribute" header="Attribute" [sortable]="true" [style]="{width:'100px'}"></p-column>
                            <p-column field="Action" header="Action" [sortable]="true" [style]="{width:'100px'}"></p-column>
                            <p-column field="FieldName" header="Field" [sortable]="true" [style]="{width:'125px'}"></p-column>                        
                            <p-column field="OldValue" header="Old Value" [sortable]="true" [style]="{width:'175px'}"></p-column>                        
                            <p-column field="NewValue" header="New Value" [sortable]="true" [style]="{width:'175px'}"></p-column>
                    </p-dataTable>      
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
          `,
    providers: [FusionService],
})

export class FusionExecutionResultsComponent extends BaseComponent {
    @Input() executionId: number;
    @Input() rowsPerPage: number = 20;

    private results: FusionExecutionResult[] = [];
    private selected: FusionExecutionResult;
    private resultCount: number = 0;
    private currentPageNumber: number = 0;
    private sortField: string = "";
    private sortOrder: SortOrder = SortOrder.None;
    private simpleTextFilter: string = "";
    private simpleSearchID: number = 0;
    private searchDelayMilliSeconds: number = 300;

    constructor(private fusionService: FusionService) {
        super();
    }
    
    private export() {
        this.fusionService.getFusionExecutionResultsExport(this.executionId, this.simpleTextFilter);
    }

    private getData() {
        this.isLoading = true;
        this.fusionService.getFusionExecutionResults(this.executionId, this.sortField, this.sortOrder, this.rowsPerPage, this.currentPageNumber, this.simpleTextFilter)
            .then(res => {
                this.results = res.results;
                this.resultCount = res.total;
                this.selected = this.results.length > 0 ? this.results[0] : null;
                this.isLoading = false;
            });
    }

    private loadResultsLazy(event: LazyLoadEvent) {
        //event.first = First row offset
        //event.rows = Number of rows per page
        //event.sortField = Field name to sort with
        //event.sortOrder = Sort order as number, 1 for asc and -1 for dec
        //filters: FilterMetadata object having field as key and filter value, filter matchMode as value        
        this.sortOrder = event.sortOrder;
        this.sortField = event.sortField == undefined ? "" : event.sortField;
        this.rowsPerPage = event.rows;
        this.currentPageNumber = event.first / event.rows;
        this.getData();
    }

    private checkSimpleSearchEnter(event, dt: DataTable) {
        if (event.keyCode == 13) this.doSimpleSearch(dt);
        else {
            if (this.simpleSearchID > 0) {
                window.clearTimeout(this.simpleSearchID);
                this.simpleSearchID = 0;
            }

            this.simpleSearchID = window.setTimeout(() => this.doSimpleSearch(dt), this.searchDelayMilliSeconds);

        }
    }

    private doSimpleSearch(dt: DataTable) {
        if (dt) dt.reset();
        this.getData();
    }
};