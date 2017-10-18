import { Input, Component, OnInit, OnDestroy} from '@angular/core';
import { MessagesService } from '../../../services/messages.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { StatisticService } from '../../../services/statistics.service';
import { RightSidebarService } from '../../../services/right-sidebar.service';
import { AdminBaseComponent } from '../admin-base.component';
import { ScoreType, ScoreTypeMetric } from '../../../models/statistic.model';
import { Title } from '@angular/platform-browser';

@Component({
    selector: 'd3s-admin-statistics-component',
    providers: [StatisticService],
    template: ` <div class="row">
                    <div class="col s12" *ngIf="showTypeEditor">
                        <div class="tile tile-detail">
                            <d3s-admin-scoretype-editor [scoretype]="selectedType" (saveClick)="saveScoreType($event)" (closeClick)="closeTypeEditor()"></d3s-admin-scoretype-editor>  
                        </div>
                    </div>
                    <div class="col s12" *ngIf="showTypeDelete">
                        <div class="tile tile-detail">
                            <d3s-delete-form
                                    [callback]="theDeleteTypeCallback"
                                    [itemId]="selectedType?.ID"
                                    [method]="'callback'"
                                    [prompt]="'Are you sure you want to delete the score type [' + [selectedType?.Name] + ']?'"                                         
                                    (onCancel)="showTypeDelete=false;"
                                ></d3s-delete-form>
                        </div>
                    </div>
                    <div class="col l3 m4 s12" *ngIf="!showTypeEditor && !showTypeDelete">                    
                        <div class="tile tile-detail">
                            <header>Score Types
                                <d3s-tile-actions [hasAdd]="true" (addClick)="addType()" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
                            </header>  
                            <d3s-loading [isLoading]="isLoading"></d3s-loading>
                            <span  *ngIf="!isLoading">
                                <input [hidden]="!showSimpleFilter" #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">                                              
                                <p-dataTable #dt [globalFilter]="gb" sortField="Name" [sortOrder]="1" [value]="scoretypes" selectionMode="single" [paginator]="true" [pageLinks]="3" [rows]="rowsPerPage" [rowsPerPageOptions]="[5,10,20]" [(selection)]="selectedType"  (onRowDblclick)="selectedType=$event.data;showEditor=true;" (onRowSelect)="selectedType=$event.data;getScoreMetrics(selectedType.ID);" >
                                    <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>
                                    <p-column field="Name" header="Name" [sortable]="true" [filter]="!showSimpleFilter"></p-column>
                                    <p-column field="Description" header="Description" [sortable]="false" [filter]="!showSimpleFilter"></p-column>
                                    <p-column [style]="{width:'40px'}">
                                        <ng-template let-analytic="rowData" pTemplate type="body">
                                            <div class="RowTools">
                                                <a style="cursor:pointer;" (click)="selectedType=analytic;showTypeEditor=true"><i class="fa fa-pencil"></i></a>                                        
                                            </div>
                                        </ng-template>
                                    </p-column>                            
                                    <p-column  [style]="{width:'40px'}">
                                        <ng-template let-analytic="rowData" pTemplate type="body">
                                            <div class="RowTools">                                
                                                <a style="cursor:pointer;" (click)="selectedType=analytic;showTypeDelete=true"><i class="fa fa-trash-o"></i></a>                                    
                                            </div>
                                        </ng-template>
                                    </p-column>    
                                </p-dataTable>      
                            </span>                            
                        </div>
                    </div>                    
                    <div class="col l9 m8 s12" *ngIf="!showTypeEditor && !showTypeDelete">
                        <div class="tile tile-detail" *ngIf="showMetricEditor">
                            <d3s-admin-scoretypemetric-editor [scoreTypeID]="selectedType?.ID" [metricID]="selectedMetric?.ID" (saveClick)="saveScoreMetric($event)" (closeClick)="closeMetricEditor()"></d3s-admin-scoretypemetric-editor>                                 
                        </div>
                        <div class="tile tile-detail" *ngIf="showMetricDelete && selectedMetric">
                            <d3s-delete-form
                                    [callback]="theDeleteMetricCallback"
                                    [itemId]="selectedMetric?.ID"
                                    [method]="'callback'"
                                    [prompt]="'Are you sure you want to delete the score metric [' + [selectedMetric?.Name] + ']?'"                                         
                                    (onCancel)="showMetricDelete=false;"
                                ></d3s-delete-form>
                        </div>
                        <div class="tile tile-detail" *ngIf="!showMetricEditor && !showMetricDelete">           
                            <header>Metrics
                                <d3s-tile-actions [hasAdd]="true" (addClick)="addMetric()" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
                            </header>  
                            <d3s-loading [isLoading]="isLoading"></d3s-loading>
                            <span  *ngIf="!isLoading">
                                <input [hidden]="!showSimpleFilter" #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
                                <p-dataTable #dt [globalFilter]="gb" sortField="ObjectName" [sortOrder]="1" [value]="metrics" selectionMode="single" [paginator]="true" [pageLinks]="3" [rows]="rowsPerPage" [rowsPerPageOptions]="[5,10,20]" [(selection)]="selectedMetric"  (onRowDblclick)="selectedMetric=$event.data;showMetricEditor=true;" >
                                    <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>
                                    <p-column field="Name" header="Name" [sortable]="true" [filter]="!showSimpleFilter"></p-column>
                                    <p-column field="ObjectName" header="Object Type" [sortable]="true" [filter]="!showSimpleFilter"></p-column>
                                    <p-column field="Description" header="Description" [sortable]="false" [filter]="!showSimpleFilter">
                                        <ng-template pTemplate type="body" let-item="rowData">
                                            <span class="truncate">{{item?.Description}}</span>
                                        </ng-template>
                                    </p-column>
                                    <p-column [style]="{width:'40px'}">
                                        <ng-template let-metric="rowData" pTemplate type="body">
                                            <div class="RowTools">
                                                <a style="cursor:pointer;" (click)="selectedMetric=metric;showMetricEditor=true"><i class="fa fa-pencil"></i></a>                                        
                                            </div>
                                        </ng-template>
                                    </p-column>                            
                                    <p-column  [style]="{width:'40px'}">
                                        <ng-template let-metric="rowData" pTemplate type="body">
                                            <div class="RowTools">                                
                                                <a style="cursor:pointer;" (click)="selectedMetric=metric;showMetricDelete=true"><i class="fa fa-trash-o"></i></a>                                    
                                            </div>
                                        </ng-template>
                                    </p-column>    
                                </p-dataTable>      
                            </span> 
                        </div>
                    <div>
                </div>  
                `
})

export class AdminStatisticsComponent extends AdminBaseComponent implements OnInit, OnDestroy {
    @Input() rowsPerPage: number = 10;

    scoretypes: ScoreType[] = [];
    selectedType: ScoreType;

    metrics: ScoreTypeMetric[] = [];
    selectedMetric: ScoreTypeMetric;

    showTypeEditor: boolean = false;
    showTypeDelete: boolean = false;

    showMetricEditor: boolean = false;
    showMetricDelete: boolean = false;

    theDeleteTypeCallback: Function;
    theDeleteMetricCallback: Function;

    constructor(rightSidebarService: RightSidebarService, private statisticService: StatisticService, protected messagesService: MessagesService, headerBreadcrumbService: HeaderBreadcrumbService, titleService: Title) {
        super(headerBreadcrumbService, titleService, rightSidebarService);        
        this.areaName = "Scoring";
        this.setCommonItems();
        this.theDeleteTypeCallback = this.deleteScoreType.bind(this);
        this.theDeleteMetricCallback = this.deleteScoreMetric.bind(this);
        this.setCommonRightSideBar(false);
    }

    ngOnInit() {
        this.getScoreTypes();
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    getScoreTypes() {
        this.isLoading = true;
        this.statisticService.getScoreTypes()
            .then(result => {
                this.scoretypes = result;
                this.isLoading = false;
                if (this.scoretypes.length > 0) {
                    this.selectedType = this.scoretypes[0];
                    this.getScoreMetrics(this.selectedType.ID);
                }
            });
    }

    getScoreMetrics(scoreTypeId: number) {
        this.isLoading = true;
        this.statisticService.getScoreTypeMetrics(scoreTypeId)
            .then(result => {
                this.metrics = result;
                this.isLoading = false;
                if (this.metrics.length > 0) this.selectedMetric = this.metrics[0];
            });
    }


    findScoreTypeIndex(id: number) {
        var index: number = -1;
        for (var analytic of this.scoretypes) {
            index++;
            if (analytic.ID == id) return index;
        }
    }

    findScoreMetricIndex(id: number) {
        var index: number = -1;
        for (var m of this.metrics) {
            index++;
            if (m.ID == id) return index;
        }
    }


    deleteScoreType(id: number) {
        this.statisticService.deleteScoreType(id).
            then(result => {
                this.showTypeDelete = false;
                this.showMessageForResult(this.messagesService, result);
                if (result.type != 'error') {
                    this.selectedType = this.scoretypes.length > 0 ? this.scoretypes[0] : null;
                    this.scoretypes.splice(this.findScoreTypeIndex(id), 1);
                }
            });
    }

    deleteScoreMetric(id: number) {
        this.statisticService.deleteScoreTypeMetric(id).
            then(result => {
                this.showMetricDelete = false;
                this.showMessageForResult(this.messagesService, result);
                if (result.type != 'error') {
                    this.selectedMetric = this.metrics.length > 0 ? this.metrics[0] : null;
                    this.metrics.splice(this.findScoreMetricIndex(id), 1);
                }
            });
    }

    saveScoreType(event) {
        this.statisticService.saveScoreType(event.scoretype)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                event.scoretype.Description = event.scoretype.Description == "null" ? "" : String(event.scoretype.Description).replace(/<(?:.|\n)*?>/gm, '');
                if (event.scoretype.ID == undefined) {
                    event.scoretype.ID = Number(result.id);
                    this.scoretypes[this.scoretypes.length] = event.scoretype;
                }
                else {
                    this.scoretypes[this.findScoreTypeIndex(event.scoretype.ID)] = event.scoretype;
                }
                this.selectedType = event.scoretype;
                this.showTypeEditor = false;
            });
    }

    saveScoreMetric(event) {
        this.statisticService.saveScoreTypeMetric(event.metric)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                if (event.metric.ID == undefined) {
                    event.metric.ID = Number(result.id);
                    this.metrics[this.metrics.length] = event.metric;
                }
                else {
                    this.metrics[this.findScoreMetricIndex(event.metric.ID)] = event.metric;
                }
                this.selectedMetric = event.metric;
                this.showMetricEditor = false;
            })
            .then(() => this.getScoreMetrics(this.selectedType.ID));
    }

    closeTypeEditor() {
        this.showTypeEditor = false;
        if (this.selectedType == null) {
            this.selectedType = this.scoretypes.length > 0 ? this.scoretypes[0] : null;
        }
    }

    closeMetricEditor() {
        this.showMetricEditor = false;
        if (this.selectedMetric == null) {
            this.selectedMetric = this.metrics.length > 0 ? this.metrics[0] : null;
        }
    }

    addType() {
        this.showTypeEditor = true;
        this.selectedType = null;
    }

    addMetric() {
        this.showMetricEditor = true;
        this.selectedMetric = null;
    }

}