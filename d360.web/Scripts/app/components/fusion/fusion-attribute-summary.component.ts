import { Input, Component, EventEmitter, Output, OnInit, OnChanges, SimpleChange } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FusionAttributeService, GridDefinitionService } from '../../services/index';
import { BaseComponent } from '../shared/base.component';
import { FusionAttributeType, FusionConfigurationDetails  } from '../../models/fusion.model';
import { LazyLoadEvent } from 'primeng/primeng';
import { FusionAttributePagedResults } from '../../models/fusion-attribute.model';
import { SortOrder } from '../../models/enums.model';
import { GridDefinition, GridColumn, GridField, GridFilterColumn, GridFilterExpression, GridRelationshipFilterExpression, GridAttributeFilterExpression } from '../../models/grid-definition.model';

@Component({
    selector: 'd3s-fusion-attribute-summary',
    template: ` 
                <div *ngIf="isLoading">
                    <div style="padding:10px;text-align:center;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                </div>
                <div class="tile tile-detail" *ngIf="!isLoading">
                    <header>Values<d3s-tile-actions [hasAdd]="false" [hasExport]="true" (exportClick)="doExport()"></d3s-tile-actions></header>
                 <!--   <input type="text" [(ngModel)]="searchValue" placeholder="Search..." style="width: 100%;">  -->
                    <p-dataTable [lazy]="true" [totalRecords]="results?.total" scrollable="true" scrollWidth="100%" [value]="results?.results" selectionMode="single" [rows]="rowsPerPage" [paginator]="true" [pageLinks]="4" [selection]="fusionAttribute" (selectionChange)="fusionAttribute=$event;fusionAttributeChange.emit(fusionAttribute);" (onLazyLoad)="loadFusionAttributesLazy($event)" [rowsPerPageOptions]="[5,10,20]" [responsive]="true" [stacked]="stacked">                                                                       
                        <p-column *ngFor="let column of columns" [field]="column.datafield" [header]="column.text" [sortable]="column.sortable"  [style]="{'width':'250px'}">
                            <template let-col let-item="rowData" pTemplate type="body">
                                <div [innerHtml]="item[column.datafield]"></div>
                            </template>
                        </p-column>                            
                    </p-dataTable>                   
                </div>
                `,
    providers: [FusionAttributeService, GridDefinitionService],
})

export class FusionAttributeSummaryComponent extends BaseComponent implements OnInit {

    @Input() fusionId: number;
    @Input() fusionAttributeTypeId: number;

    @Input() fusionAttribute: any;
    @Output() fusionAttributeChange = new EventEmitter();

    
    
    private totalRecords: number;
    private rowsPerPage: number = 10;
    private results: FusionAttributePagedResults;
    columns: GridColumn[] = [];    
    filtercolumns: GridFilterColumn[] = [];

    currentPageNumber: number = 0;
    sortField: string = "";
    sortOrder: SortOrder = SortOrder.None;

    
    constructor(private gridDefinitionService: GridDefinitionService, private fusionAttributeService: FusionAttributeService) {
        super();
    }

    ngOnInit() {

    }
    
    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes['fusionAttributeTypeId'] && this.fusionAttributeTypeId) {            
            this.getFieldsDefinition();
        }
    }

    getFieldsDefinition() {
        this.isLoading = true;
        this.gridDefinitionService.getGridDefinition(this.fusionAttributeTypeId, 'FusionAttributeType')
            .then(result => {
                if (result) {
                    this.columns = result.Columns;
                    this.filtercolumns = result.FilterColumns;
                }                
                this.isLoading = false;
            });
    }

    private getData() {        
        if (!this.fusionId || !this.fusionAttributeTypeId) {
            console.log("ERROR - NO FUSION ATTRIBUTE TYPE ID SPECIFIED OR FUSION ID");
            return;
        }

        this.fusionAttributeService.getFusionAttributes(this.fusionId, this.fusionAttributeTypeId, this.currentPageNumber, this.rowsPerPage, this.sortField, this.sortOrder)
            .then(res => {
                this.results = res;

                if (!this.fusionAttribute && this.results && this.results.results && this.results.results.length > 0) {
                    this.fusionAttribute = this.results.results[0];
                    this.fusionAttributeChange.emit(this.fusionAttribute);
                }
            });
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
        this.fusionAttributeService.getFusionAttributeExcel(this.fusionId, this.fusionAttributeTypeId);
    }
};