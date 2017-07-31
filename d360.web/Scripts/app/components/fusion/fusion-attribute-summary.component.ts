import { Input, Component, EventEmitter, Output, OnChanges, SimpleChange, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { FusionAttributeService } from '../../services/fusion-attribute.service';
import { MessagesService } from '../../services/messages.service';
import { GridDefinitionService } from '../../services/grid-definition.service';
import { BaseComponent } from '../shared/base.component';
import { FusionAttributeType, FusionConfigurationDetails  } from '../../models/fusion.model';
import { LazyLoadEvent } from 'primeng/primeng';
import { FusionAttributePagedResults, FusionAttributeFilter } from '../../models/fusion-attribute.model';
import { SortOrder } from '../../models/enums.model';
import { GridDefinition, GridColumn, GridField, GridFilterColumn, GridFilterExpression, GridRelationshipFilterExpression, GridAttributeFilterExpression } from '../../models/grid-definition.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { StateService } from '../../services/state.service';

@Component({
    selector: 'd3s-fusion-attribute-summary',
    template: `                 
                <div class="tile tile-detail" style="position:initial">
                    <header>Values<d3s-tile-actions [hasAdd]="false" [hasExport]="true" (exportClick)="doExport()"></d3s-tile-actions></header>
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <span *ngIf="!isLoading && !showEditor">
                        <d3s-fusion-attribute-summary-filters [filterColumns]="filtercolumns" [filters]="stateService.fusionFilters.filters" (filtersChange)="doFilterResults($event)" [isFiltering]="isFiltering"></d3s-fusion-attribute-summary-filters>                 
                        <p-dataTable #dt resizableColumns="true" columnResizeMode="expand" [lazy]="true" [totalRecords]="results?.total" [value]="results?.results" selectionMode="single" [rows]="stateService.fusionFilters.rowsPerPage" paginator="true" pageLinks="3" [selection]="fusionAttribute" (selectionChange)="fusionAttribute=$event;fusionAttributeChange.emit(fusionAttribute);" (onLazyLoad)="loadFusionAttributesLazy($event)" [rowsPerPageOptions]="defaultPagingOptions">                                                        
                           <p-column [style]="{width:'30px'}">
                                    <ng-template let-item="rowData" pTemplate type="body">
                                        <div class="RowTools">
                                            <a style="cursor:pointer;" (click)="selectItem(item)" title="details"><i class="fa fa-info" aria-hidden="true"></i></a> 
                                        </div>
                                    </ng-template>
                            </p-column>                                                        
                           <p-column [style]="{width:'30px'}">
                                    <ng-template let-item="rowData" pTemplate type="body">
                                        <div class="RowTools" *ngIf="item.IsEditable">
                                            <a style="cursor:pointer;" (click)="fusionAttribute=item;showEditor=true;"><i class="fa fa-pencil"></i></a>
                                        </div>
                                    </ng-template>
                            </p-column>
                            <p-column *ngFor="let column of columns;let first = first" [field]="column.datafield" [header]="column.text" [sortable]="column.sortable"  [style]="{'width':'250px'}">
                                <ng-template let-col let-item="rowData" pTemplate type="body">
                                    <a *ngIf="first && item[column.datafield]" (click)="selectItem(item)">{{item[column.datafield]}}</a>
                                    <span *ngIf="!first && item[column.datafield]" [innerHtml]="item[column.datafield]"></span>
                                </ng-template>
                            </p-column>                            
                        </p-dataTable>                   
                        <div class="center" style="font-weight:bold"><d3s-grid-paging-info *ngIf="dt && dt.totalRecords" [totalRecords]="dt?.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></div>
                    </span>
                    <d3s-dynamic-editor *ngIf="showEditor" [newActionName]="newActionName" [objectID]="fusionAttributeTypeId" objectType="FusionAttribute" [title]="'Item'" [selection]="fusionAttribute" [rowID]="'ID'" (saveClick)="saveItem($event)" (closeClick)="closeEditor()"></d3s-dynamic-editor>
                </div>
                `,
    providers: [FusionAttributeService, GridDefinitionService],
    changeDetection: ChangeDetectionStrategy.OnPush,
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
       
    private results: FusionAttributePagedResults;
    columns: GridColumn[] = [];
    filtercolumns: GridFilterColumn[] = [];
    private isFiltering: boolean = false;
    showEditor: boolean = false;
    
    constructor(private gridDefinitionService: GridDefinitionService,
        private fusionAttributeService: FusionAttributeService,
        private messagesService: MessagesService,
        private router: Router,
        private stateService: StateService,
        private changeDetectorRef: ChangeDetectorRef
    ) {
        super();
    }
        
    ngOnChanges(changes: { [propName: string]: SimpleChange }) {        
        if (changes['fusionAttributeTypeId'] && this.fusionAttributeTypeId) {            
            this.fusionObject = 'FusionAttributeType';
            this.fusionObjectID = this.fusionAttributeTypeId;
            this.fusionQueryAttributeTypeId = null;
            if (this.initialFusionAttributeId > 0)
                this.stateService.fusionFilters.filters = [{ dataField: 'ID', value: this.initialFusionAttributeId.toString(), condition: 'CONTAINS', columnType: '' }];

            this.stateService.resetFusionAttributeFilterIfRequired(this.fusionObject, this.fusionObjectID);      

            this.getFieldsDefinition();            
        } 
        else if (changes['fusionQueryAttributeTypeId'] && this.fusionQueryAttributeTypeId) {
            this.fusionObject = 'FusionQueryAttributeType';            
            this.fusionObjectID = this.fusionQueryAttributeTypeId;
            this.fusionAttributeTypeId = null;            
            this.stateService.resetFusionAttributeFilterIfRequired(this.fusionObject, this.fusionObjectID);      

            this.getFieldsDefinition();            
        } 
        else if (changes['initialFusionAttributeId'] && this.initialFusionAttributeId && this.fusionAttributeTypeId) 
        {
            this.fusionObject = 'FusionAttributeType';
            this.fusionObjectID = this.fusionAttributeTypeId;
            this.fusionQueryAttributeTypeId = null;
            if (this.initialFusionAttributeId > 0)
                this.stateService.fusionFilters.filters = [{ dataField: 'ID', value: this.initialFusionAttributeId.toString(), condition: 'CONTAINS', columnType: '' }];

            this.stateService.resetFusionAttributeFilterIfRequired(this.fusionObject, this.fusionObjectID);

            this.getFieldsDefinition();            
        }
    }

    closeEditor() {
        this.showEditor = false;
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
                this.changeDetectorRef.markForCheck();
            });
    }

    private doFilterResults(event) {        
        this.stateService.fusionFilters.filters = event;
        this.stateService.fusionFilters.currentPageNumber = 0;        
        this.getData();
    }

    private getData() {
        if (!this.fusionId || !this.fusionObjectID) {
            console.log("ERROR - NO FUSION ATTRIBUTE TYPE ID SPECIFIED OR FUSION ID");
            return;
        }

        //remove any invalid filters
        if (this.stateService.fusionFilters.filters && this.stateService.fusionFilters.filters.length > 0) {
            for (var i = this.stateService.fusionFilters.filters.length - 1; i >= 0; i--) {
                if (!this.stateService.fusionFilters.filters[i].dataField || !this.stateService.fusionFilters.filters[i].value) {
                    console.log("REMOVING FILTER", i);
                    this.stateService.fusionFilters.filters.splice(i, 1);
                }
            }
        }
        this.isFiltering = true;
        if (this.fusionObject == "FusionQueryAttributeType") {
            this.fusionAttributeService.getFusionQueryAttributes(this.fusionId, this.fusionObjectID, this.stateService.fusionFilters.currentPageNumber, this.stateService.fusionFilters.rowsPerPage, this.stateService.fusionFilters.sortField, this.stateService.fusionFilters.sortOrder, this.stateService.fusionFilters.filters)
                .then(res => {
                    this.results = res;
                    this.isFiltering = false;
                    if (!this.fusionAttribute && this.results && this.results.results && this.results.results.length > 0) {
                        this.fusionAttribute = this.results.results[0];
                        
                        this.fusionAttributeChange.emit(this.fusionAttribute);                        
                    }
                    this.changeDetectorRef.markForCheck();
                });
        }
        else {
            this.fusionAttributeService.getFusionAttributes(this.fusionId, this.fusionObjectID, this.stateService.fusionFilters.currentPageNumber, this.stateService.fusionFilters.rowsPerPage, this.stateService.fusionFilters.sortField, this.stateService.fusionFilters.sortOrder, this.stateService.fusionFilters.filters)
                .then(res => {
                    this.results = res;
                    this.isFiltering = false;
                    if (!this.fusionAttribute && this.results && this.results.results && this.results.results.length > 0) {
                        this.fusionAttribute = this.results.results[0];
                       
                        this.fusionAttributeChange.emit(this.fusionAttribute);                        
                    }
                    this.changeDetectorRef.markForCheck();
                });
        }
    }

    private loadFusionAttributesLazy(event: LazyLoadEvent) {
        //event.first = First row offset
        //event.rows = Number of rows per page
        //event.sortField = Field name to sort with
        //event.sortOrder = Sort order as number, 1 for asc and -1 for dec
        //filters: FilterMetadata object having field as key and filter value, filter matchMode as value

        this.stateService.fusionFilters.sortOrder = event.sortOrder;
        this.stateService.fusionFilters.sortField = event.sortField == undefined ? "" : event.sortField;
        this.stateService.fusionFilters.rowsPerPage = event.rows;
        this.stateService.fusionFilters.currentPageNumber = event.first / event.rows;
        
        this.getData();
    }

    private doExport() {
        this.fusionAttributeService.getFusionAttributeExcel(this.fusionObject, this.fusionId, (this.fusionObject == "FusionQueryAttributeType") ? this.fusionQueryAttributeTypeId : this.fusionAttributeTypeId, this.stateService.fusionFilters.sortField, this.stateService.fusionFilters.sortOrder, this.stateService.fusionFilters.filters);        
    }

    saveItem(event) {
        this.isLoading = true;
        this.showEditor = false;
        this.fusionAttributeService.saveAttribute(event.item)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                this.getData();
                this.isLoading = false;
            });
    }

    private selectItem(item) {        
        this.router.navigateByUrl(SiteUrlHelpers.SITE_URL_FUSION_ROOT + '/' + SiteUrlHelpers.SITE_URL_FUSION_ATTRIBUTE_DETAILS + '/' + item.Type + '/' + item.ID + '/' + (item.Name ? encodeURIComponent(item.Name) : 'Fusion Query Attribute'));
    }
};