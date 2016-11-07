import { Input, Component, EventEmitter, Output, OnChanges, SimpleChange } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FusionAttributeService, GridDefinitionService } from '../../services/index';
import { BaseComponent } from '../shared/base.component';
import { FusionAttributeType, FusionConfigurationDetails  } from '../../models/fusion.model';
import { LazyLoadEvent } from 'primeng/primeng';
import { FusionAttributePagedResults, FusionAttributeFilter } from '../../models/fusion-attribute.model';
import { SortOrder } from '../../models/enums.model';
import { GridDefinition, GridColumn, GridField, GridFilterColumn, GridFilterExpression, GridRelationshipFilterExpression, GridAttributeFilterExpression } from '../../models/grid-definition.model';

@Component({
    selector: 'd3s-fusion-attribute-summary',
    template: `                 
                <div class="tile tile-detail">
                    <header>Values<d3s-tile-actions [hasAdd]="false" [hasExport]="true" (exportClick)="doExport()"></d3s-tile-actions></header>
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <span *ngIf="!isLoading">
                        <d3s-fusion-attribute-summary-filters [filterColumns]="filtercolumns" [filters]="filters" (filtersChange)="doFilterResults($event)"></d3s-fusion-attribute-summary-filters>                 
                        <p-dataTable [lazy]="true" [totalRecords]="results?.total" scrollable="true" scrollWidth="100%" [value]="results?.results" selectionMode="single" [rows]="rowsPerPage" paginator="true" pageLinks="3" [selection]="fusionAttribute" (selectionChange)="fusionAttribute=$event;fusionAttributeChange.emit(fusionAttribute);" (onLazyLoad)="loadFusionAttributesLazy($event)" [rowsPerPageOptions]="defaultPagingOptions">
                            <p-column *ngFor="let column of columns" [field]="column.datafield" [header]="column.text" [sortable]="column.sortable"  [style]="{'width':'250px'}">
                                <template let-col let-item="rowData" pTemplate type="body">
                                    <span [innerHtml]="item[column.datafield]" class="truncate" style="display:inline-block;width:245px"></span>
                                </template>
                            </p-column>                            
                        </p-dataTable>                   
                    </span>
                </div>
                `,
    providers: [FusionAttributeService, GridDefinitionService],
})

export class FusionAttributeSummaryComponent extends BaseComponent implements OnChanges {

    @Input() fusionId: number;
    @Input() fusionAttributeTypeId: number;
    @Input() fusionQueryAttributeTypeId: number;

    @Input() fusionAttribute: any;
    @Output() fusionAttributeChange = new EventEmitter();
    @Input() initialFusionAttributeId: number;

    @Input() fusionQueryAttribute: any;
    @Output() fusionQueryAttributeChange = new EventEmitter();
    @Input() initialFusionQueryAttributeId: number;

    private fusionObject: string = 'FusionAttributeType';
    private fusionObjectID: number = 0;
    private filters: FusionAttributeFilter[] = [];
                
    private rowsPerPage: number = this.defaultInitialItemsPerPage;
    private results: FusionAttributePagedResults;    
    columns: GridColumn[] = [];    
    filtercolumns: GridFilterColumn[] = [];

    currentPageNumber: number = 0;
    sortField: string = "";
    sortOrder: SortOrder = SortOrder.None;

    
    constructor(private gridDefinitionService: GridDefinitionService, private fusionAttributeService: FusionAttributeService) {
        super();
    }
        
    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes['fusionAttributeTypeId'] && this.fusionAttributeTypeId) {
            this.fusionObject = 'FusionAttributeType';
            this.fusionObjectID = this.fusionAttributeTypeId;
            this.fusionQueryAttributeTypeId = null;
            if (this.initialFusionAttributeId > 0)
                this.filters = [{ dataField: 'ID', value: this.initialFusionAttributeId.toString(), condition: 'CONTAINS' }];
            else   
                this.filters = [];

            this.getFieldsDefinition();
        } 
        else if (changes['fusionQueryAttributeTypeId'] && this.fusionQueryAttributeTypeId) {
            this.fusionObject = 'FusionQueryAttributeType';
            console.log(this.fusionQueryAttributeTypeId);
            this.fusionObjectID = this.fusionQueryAttributeTypeId;
            this.fusionAttributeTypeId = null;
            this.filters = [];
            this.getFieldsDefinition();
        } 
    }
    
    getFieldsDefinition() {
        this.isLoading = true;

        this.gridDefinitionService.getGridDefinition(this.fusionObjectID, this.fusionObject, this.fusionId, 'FusionID')
            .then(result => {
                if (result) {
                    this.columns = result.Columns;
                    this.filtercolumns = result.FilterColumns;
                }                
                this.isLoading = false;
            });
    }

    private doFilterResults(event) {        
        this.filters = event;
        this.currentPageNumber = 0;        
        this.getData();
    }

    private getData() {
        if (!this.fusionId || !this.fusionObjectID) {
            console.log("ERROR - NO FUSION ATTRIBUTE TYPE ID SPECIFIED OR FUSION ID");
            return;
        }

        //remove any invalid filters
        if (this.filters && this.filters.length > 0) {
            for (var i = this.filters.length - 1; i >= 0; i--) {
                if (!this.filters[i].dataField || !this.filters[i].value) {
                    console.log("REMOVING FILTER", i);
                    this.filters.splice(i, 1);
                }
            }
        }

        if (this.fusionObject == "FusionQueryAttributeType") {
            this.fusionAttributeService.getFusionQueryAttributes(this.fusionId, this.fusionObjectID, this.currentPageNumber, this.rowsPerPage, this.sortField, this.sortOrder, this.filters)
                .then(res => {
                    this.results = res;

                    if (!this.fusionAttribute && this.results && this.results.results && this.results.results.length > 0) {
                        this.fusionAttribute = this.results.results[0];
                        this.fusionAttributeChange.emit(this.fusionAttribute);
                    }
                });
        }
        else {
            this.fusionAttributeService.getFusionAttributes(this.fusionId, this.fusionObjectID, this.currentPageNumber, this.rowsPerPage, this.sortField, this.sortOrder, this.filters)
                .then(res => {
                    this.results = res;

                    if (!this.fusionAttribute && this.results && this.results.results && this.results.results.length > 0) {
                        this.fusionAttribute = this.results.results[0];
                        this.fusionAttributeChange.emit(this.fusionAttribute);
                    }
                });
        }
    }


    private loadFusionAttributesLazy(event: LazyLoadEvent) {
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

    private doExport() {
        if (this.fusionObject == "FusionQueryAttributeType") {
            this.fusionAttributeService.getFusionQueryAttributeExcel(this.fusionId, this.fusionQueryAttributeTypeId);
        }
        else {
            this.fusionAttributeService.getFusionAttributeExcel(this.fusionId, this.fusionAttributeTypeId);
        }
    }
};