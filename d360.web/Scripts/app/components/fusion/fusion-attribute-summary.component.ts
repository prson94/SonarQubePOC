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
                    <header *ngIf ="!hideHeader">Values<d3s-tile-actions [hasAdd]="false" [hasExport]="true" (exportClick)="doExport()"></d3s-tile-actions></header>
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <span *ngIf="!isLoading && !showEditor">
                        <d3s-fusion-attribute-summary-filters [filterColumns]="filtercolumns" [filters]="stateService.getFusionFilter(isDataProfile).filters" (filtersChange)="doFilterResults($event)" [isFiltering]="isFiltering"></d3s-fusion-attribute-summary-filters>                 
                        <p-table #dt [value]="results?.results" selectionMode="single" [resizableColumns]="true" [lazy]="true" [totalRecords]="results?.total" [metaKeySelection]="true" 
                            [globalFilterFields]="[]" [pageLinks]="3" [paginator]="true" [rows]="stateService.getFusionFilter(isDataProfile).rowsPerPage" [rowsPerPageOptions]="defaultPagingOptions"
                            [selection]="fusionAttribute" (selectionChange)="fusionAttribute=$event;fusionAttributeChange.emit(fusionAttribute);" (onLazyLoad)="loadFusionAttributesLazy($event)"
                            [style]="{'padding-bottom':'80px'}">
                            <ng-template pTemplate="header">
                                <tr>
                                    <th style="width: 30px; cursor: default"></th>
                                    <th style="width: 30px; cursor: default"></th>
                                    <th *ngFor="let col of columns" 
                                        pResizableColumn 
                                        [style.width]="col.filterable == 'bool' ? '250px' : '200px'" 
                                        [style.cursor]="col.filtertype == 'bool' ? null : 'default'"
                                        [pSortableColumn]="col.sortable ? col.datafield : null">
                                        {{col.text}}
                                        <d3s-sortIcon *ngIf="col.sortable" [field]="col.datafield"></d3s-sortIcon>
                                    </th>
                                </tr>
                            </ng-template>
                            <ng-template pTemplate="body" let-item>
                                <tr [pSelectableRow]="item">
                                    <td>
                                        <div class="RowTools">
                                            <d3s-preview-tooltip objectType="FusionAttribute" [objectId]="item.ID" icon="info">
                                                <a style="cursor:pointer;" (click)="selectItem(item)" title="details"></a>
                                            </d3s-preview-tooltip>
                                        </div>
                                    </td>
                                    <td>
                                        <div class="RowTools" *ngIf="item.IsEditable">
                                            <a style="cursor:pointer;" (click)="fusionAttribute=item;showEditor=true;fusionAttributeChange.emit(fusionAttribute);"><i class="fa fa-pencil"></i></a>
                                        </div>
                                    </td>
                                    <td *ngFor="let col of columns">
                                        <ng-container *ngIf="col.filtertype == 'bool'; else elseContent">
                                            <span><i *ngIf="item[col.datafield]=='true'" class="fa fa-check enabled" title="True"></i></span>
                                            <span><i *ngIf="item[col.datafield]=='false'" class="fa fa-times disabled" title="False"></i></span>
                                        </ng-container>
                                        <ng-template #elseContent>
                                              <a *ngIf="item[col.datafield]" (click)="selectItem(item)">
                                                <d3s-dynamic-field-value [column]="col" [fields]="fields" [item]="item">
                                                </d3s-dynamic-field-value> 
                                            </a> 
                                        </ng-template>
                                    </td>
                                </tr>
                            </ng-template>
                            <ng-template pTemplate="summary">
                                <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                            </ng-template>
                        </p-table>
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

    @Input() hideHeader: boolean = false;
    @Input() isDataProfile: boolean = false;

    private fusionObject: string = 'FusionAttributeType';
    private fusionObjectID: number = 0;
       
    private results: FusionAttributePagedResults;
    columns: GridColumn[] = [];
    fields: GridField[] = [];
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

            this.stateService.resetFusionAttributeFilterIfRequired(this.fusionObject, this.fusionObjectID,this.isDataProfile);     

            if (this.initialFusionAttributeId > 0)
                this.stateService.getFusionFilter(this.isDataProfile).filters = [{ dataField: 'ID', value: this.initialFusionAttributeId.toString(), condition: 'CONTAINS', columnType: '' }];

            this.getFieldsDefinition();  
            
        } 
        else if (changes['fusionQueryAttributeTypeId'] && this.fusionQueryAttributeTypeId) {
            this.fusionObject = 'FusionQueryAttributeType';            
            this.fusionObjectID = this.fusionQueryAttributeTypeId;
            this.fusionAttributeTypeId = null;            
            this.stateService.resetFusionAttributeFilterIfRequired(this.fusionObject, this.fusionObjectID,this.isDataProfile);      

            this.getFieldsDefinition();            
        } 
        else if (changes['initialFusionAttributeId'] && this.initialFusionAttributeId && this.fusionAttributeTypeId) 
        {
            this.fusionObject = 'FusionAttributeType';
            this.fusionObjectID = this.fusionAttributeTypeId;
            this.fusionQueryAttributeTypeId = null;
            if (this.initialFusionAttributeId > 0)
                this.stateService.getFusionFilter(this.isDataProfile).filters = [{ dataField: 'ID', value: this.initialFusionAttributeId.toString(), condition: 'CONTAINS', columnType: '' }];

            this.stateService.resetFusionAttributeFilterIfRequired(this.fusionObject, this.fusionObjectID,this.isDataProfile);

            this.getFieldsDefinition();            
        }
    }

    closeEditor() {
        this.showEditor = false;
    }

    getFieldsDefinition() {
        this.isLoading = true;
        let target: string = this.isDataProfile ? 'DataProfile' : 'FusionID';
        this.gridDefinitionService.getGridDefinition(this.fusionObjectID, this.fusionObject, this.fusionId, target)
            .then(result => {
                if (result) {
                    this.columns = result.Columns;
                    this.fields = result.Fields;
                    this.filtercolumns = result.FilterColumns;
                }                
                this.isLoading = false;
                this.changeDetectorRef.markForCheck();
            });
    }

    private doFilterResults(event) {        
        this.stateService.getFusionFilter(this.isDataProfile).filters = event;
        this.stateService.getFusionFilter(this.isDataProfile).currentPageNumber = 0;     
        this.fusionAttribute = null; //reseting the selected row
        this.getData();
    }

    private getData() {
        if ((this.fusionId === undefined) || !this.fusionObjectID) {
            console.log("ERROR - NO FUSION ATTRIBUTE TYPE ID SPECIFIED OR FUSION ID");
            return;
        }

        //remove any invalid filters
        if (this.stateService.getFusionFilter(this.isDataProfile).filters && this.stateService.getFusionFilter(this.isDataProfile).filters.length > 0) {
            for (var i = this.stateService.getFusionFilter(this.isDataProfile).filters.length - 1; i >= 0; i--) {
                if (!this.stateService.getFusionFilter(this.isDataProfile).filters[i].dataField || !this.stateService.getFusionFilter(this.isDataProfile).filters[i].value) {
                    console.log("REMOVING FILTER", i);
                    this.stateService.getFusionFilter(this.isDataProfile).filters.splice(i, 1);
                }
            }
        }
        this.isFiltering = true;
   
        if (this.fusionObject == "FusionQueryAttributeType") {
            this.fusionAttributeService.getFusionQueryAttributes(this.fusionId, this.fusionObjectID, this.stateService.getFusionFilter(this.isDataProfile).currentPageNumber, this.stateService.getFusionFilter(this.isDataProfile).rowsPerPage, this.stateService.getFusionFilter(this.isDataProfile).sortField, this.stateService.getFusionFilter(this.isDataProfile).sortOrder, this.stateService.getFusionFilter(this.isDataProfile).filters)
                .then(res => {
                    this.results = res;
                    this.isFiltering = false;
                    if (!this.fusionAttribute && this.results && this.results.results && this.results.results.length > 0) {
                        this.fusionAttribute = this.results.results[0];
                        this.fusionAttributeChange.emit(this.fusionAttribute);
                    } else {
                        this.fusionAttribute = null;
                    }
                    this.fusionAttributeChange.emit(this.fusionAttribute);
                    this.changeDetectorRef.markForCheck();

                });
        }
        else {
            let target: string = this.isDataProfile ? 'DataProfile' : '';
            this.fusionAttributeService.getFusionAttributes(this.fusionId, this.fusionObjectID, target, this.stateService.getFusionFilter(this.isDataProfile).currentPageNumber, this.stateService.getFusionFilter(this.isDataProfile).rowsPerPage, this.stateService.getFusionFilter(this.isDataProfile).sortField, this.stateService.getFusionFilter(this.isDataProfile).sortOrder, this.stateService.getFusionFilter(this.isDataProfile).filters)
                .then(res => {
                    this.results = res;
                    this.isFiltering = false;
                    if (!this.fusionAttribute && this.results && this.results.results && this.results.results.length > 0) {
                        this.fusionAttribute = this.results.results[0];
                    } else {
                        this.fusionAttribute = null;
                    }
                    this.fusionAttributeChange.emit(this.fusionAttribute);
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
        this.fusionAttribute = null; //reseting the selected row
        this.stateService.getFusionFilter(this.isDataProfile).sortOrder = event.sortOrder;
        this.stateService.getFusionFilter(this.isDataProfile).sortField = event.sortField == undefined ? "" : event.sortField;
        this.stateService.getFusionFilter(this.isDataProfile).rowsPerPage = event.rows;
        this.stateService.getFusionFilter(this.isDataProfile).currentPageNumber = event.first / event.rows;
        
        this.getData();
    }

    private doExport() {
        this.isLoading = true;
        this.fusionAttributeService.getFusionAttributeExcel(this.fusionObject, this.fusionId, (this.fusionObject == "FusionQueryAttributeType") ? this.fusionQueryAttributeTypeId : this.fusionAttributeTypeId, this.stateService.getFusionFilter(this.isDataProfile).sortField, this.stateService.getFusionFilter(this.isDataProfile).sortOrder, this.stateService.getFusionFilter(this.isDataProfile).filters)
            .then(res => {                
                this.isLoading = false;
                this.changeDetectorRef.markForCheck();
            });
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