import { Input, Component, OnInit, OnDestroy} from '@angular/core';
import { MessagesService, HeaderBreadcrumbService, StatisticService, RightSidebarService  } from '../../../services/index';
import { AdminBaseComponent } from '../admin-base.component';
import { StatisticType } from '../../../models/statistic.model';
import { Title } from '@angular/platform-browser';

@Component({
    selector: 'd3s-admin-statistics-component',
    providers: [StatisticService],
    template: ` <d3s-audit *ngIf="isAuditVisible" [objectID]="selected?.ID" [objectName]="selected?.Name" [objectType]="'StatisticType'"></d3s-audit>
                <div *ngIf="!isAuditVisible" class="row">
                    <div class="col s12" *ngIf="showEditor">
                        <div class="tile tile-detail">
                            <d3s-admin-statistic-editor [statisticID]="selected?.ID" (saveClick)="saveStatisticType($event)" (closeClick)="closeEditor()"></d3s-admin-statistic-editor>                                 
                        </div>
                    </div>
                    <div class="col s12" *ngIf="showDelete">
                        <div class="tile tile-detail">
                            <d3s-delete-form
                                    [callback]="theDeleteCallback"
                                    [itemId]="selected?.ID"
                                    [method]="'callback'"
                                    [prompt]="'Are you sure you want to delete the Analytic type [' + [selected?.Name] + ']?'"                                         
                                    (onCancel)="showDelete=false;"
                                ></d3s-delete-form>
                        </div>
                    </div>
                    <div class="col l6 s12" *ngIf="!showEditor && !showDelete">                    
                        <div class="tile tile-detail">
                            <header>Analytic Types
                                <d3s-tile-actions [hasAdd]="true" (addClick)="add()" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
                            </header>  
                            <d3s-loading [isLoading]="isLoading"></d3s-loading>
                            <span  *ngIf="!isLoading">
                                <input [hidden]="!showSimpleFilter" #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">                                              
                                <p-dataTable #dt [globalFilter]="gb" sortField="ObjectName" [sortOrder]="1" [value]="statistics" selectionMode="single" [paginator]="true" [pageLinks]="3" [rows]="rowsPerPage" [rowsPerPageOptions]="[5,10,20]" [(selection)]="selected"  (onRowDblclick)="selected=$event.data;showEditor=true;" >
                                    <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                                    <p-column field="ObjectName" header="Object" [sortable]="true" [filter]="!showSimpleFilter"></p-column>                                                        
                                    <p-column field="Name" header="Name" [sortable]="true" [filter]="!showSimpleFilter"></p-column>                                                        
                                    <p-column field="Score" header="Score" [sortable]="true" [filter]="!showSimpleFilter"></p-column>                                                        
                                    <p-column [style]="{width:'40px'}">
                                        <template let-analytic="rowData" pTemplate type="body">
                                            <div class="RowTools">
                                                <a style="cursor:pointer;" (click)="selected=analytic;showEditor=true"><i class="fa fa-pencil"></i></a>                                        
                                            </div>
                                        </template>
                                    </p-column>                            
                                    <p-column  [style]="{width:'40px'}">
                                        <template let-analytic="rowData" pTemplate type="body">
                                            <div class="RowTools">                                
                                                <a style="cursor:pointer;" (click)="selected=analytic;showDelete=true"><i class="fa fa-trash-o"></i></a>                                    
                                            </div>
                                        </template>
                                    </p-column>    
                                </p-dataTable>      
                            </span>                            
                        </div>
                    </div>                    
                    <div class="col l6 s12" *ngIf="!showEditor && !showDelete && selected">                        
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">           
                                    <object-detail [objectType]="'StatisticType'" [objectID]="selected?.ID"></object-detail>
                                </div>
                            </div>
                        </div>
                    <div>
                </div>  
                `
})

export class AdminStatisticsComponent extends AdminBaseComponent implements OnInit, OnDestroy {
    @Input() rowsPerPage: number = 10;

    statistics: StatisticType[] = [];
    selected: StatisticType;
    showEditor: boolean = false;
    showDelete: boolean = false;
    theDeleteCallback: Function;

    constructor(rightSidebarService: RightSidebarService, private statisticService: StatisticService, protected messagesService: MessagesService, headerBreadcrumbService: HeaderBreadcrumbService, titleService: Title) {
        super(headerBreadcrumbService, titleService, rightSidebarService);        
        this.areaName = "Analytic Types";
        this.setCommonItems();
        this.theDeleteCallback = this.deleteStatisticType.bind(this);
        this.setCommonRightSideBar();
    }

    ngOnInit() {
        this.getStatistics();
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    getStatistics() {
        this.isLoading = true;
        this.statisticService.getStatistics()
            .then(result => {
                this.statistics = result;
                this.isLoading = false;
                if (this.statistics.length > 0) this.selected = this.statistics[0];
            });
    }

    findStatisticTypeIndex(id: number) {
        var index: number = -1;
        for (var analytic of this.statistics) {
            index++;
            if (analytic.ID == id) return index;
        }
    }

    deleteStatisticType(id: number) {
        this.statisticService.deleteStatistic(id).
            then(result => {
                this.showDelete = false;
                this.showMessageForResult(this.messagesService, result);
                if (result.type != 'error') {
                    this.selected = this.statistics.length > 0 ? this.statistics[0] : null;
                    this.statistics.splice(this.findStatisticTypeIndex(id), 1);
                }
            });
    }

    saveStatisticType(event) {
        this.statisticService.saveStatistic(event.statistic)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                if (event.statistic.ID == undefined) {
                    event.statistic.ID = Number(result.id);
                    this.statistics[this.statistics.length] = event.statistic;
                }
                else {
                    this.statistics[this.findStatisticTypeIndex(event.statistic.ID)] = event.statistic;
                }
                this.selected = event.statistic;
                this.showEditor = false;
            });
    }

    closeEditor() {
        this.showEditor = false;
        if (this.selected == null) {
            this.selected = this.statistics.length > 0 ? this.statistics[0] : null;
        }
    }

    add() {
        this.showEditor = true;
        this.selected = null;
    }

}