///<reference path="../../es6-shim.d.ts"/>
import { Component} from '@angular/core';
import {DataTable, Column} from 'primeng/primeng';
import { MessagesService, HeaderBreadcrumbService, PageHeader, StatisticService  } from '../../services/index';
import { AdminBaseComponent } from './admin-base.component';
import { TileActionsComponent } from '../tiles/tile-actions.component';
import { StatisticType } from '../../models/statistic.model';
import { DynamicEditorComponent } from '../shared/dynamic-editor.component';
import { ObjectDetailTile } from '../tiles/object-detail.tile';


@Component({
    selector: 'd3s-admin-analytics-component',
    directives: [DataTable, Column, TileActionsComponent, ObjectDetailTile],
    providers: [StatisticService],
    template: `<div class="row">
                    <div class="col l4 s12">                    
                        <div class="tile tile-detail">
                            <header *ngIf="!showEditor && !showDelete">Analytic Types
                                <d3s-tile-actions [hasAdd]="true" [addTitle]="'Add Analytic Type'" (addClick)="add()"></d3s-tile-actions>                            
                            </header>  
                            <div *ngIf="isLoading">
                                <div style="padding:10px;text-align:center;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                            </div>                          
                            <p-dataTable *ngIf="!isLoading && !showEditor && !showDelete" [value]="statistics" selectionMode="single" [rows]="20" [paginator]="true" [pageLinks]="3" expandableRows="true" [(selection)]="selected"  (onRowDblclick)="selected=$event.data;showEditor=true;" >
                                <p-column field="ObjectName" header="Object" [sortable]="true" [filter]="true"></p-column>                                                        
                                <p-column field="Name" header="Name" [sortable]="true" [filter]="true"></p-column>                                                        
                                <p-column field="Score" header="Score" [sortable]="true" [filter]="true"></p-column>                                                        
                                <p-column [style]="{width:'40px'}">
                                    <template let-analytic="rowData">
                                        <div class="RowTools">
                                            <a style="cursor:pointer;" (click)="selected=analytic;showEditor=true"><i class="fa fa-pencil"></i></a>                                        
                                        </div>
                                    </template>
                                </p-column>                            
                                <p-column  [style]="{width:'40px'}">
                                    <template let-analytic="rowData">
                                        <div class="RowTools">                                
                                            <a style="cursor:pointer;" (click)="selected=analytic;showDelete=true"><i class="fa fa-trash-o"></i></a>                                    
                                        </div>
                                    </template>
                                </p-column>    
                            </p-dataTable>      
                            <d3s-dynamic-editor *ngIf="showEditor" [objectID]="selected?.ID" [objectType]="'StatisticType'" [title]="'Analytic Type'" [selection]="selected" (saveClick)="saveAnalyticType($event)" (closeClick)="closeEditor()"></d3s-dynamic-editor>     
                            <delete-form *ngIf="showDelete"
                                [callback]="theDeleteCallback"
                                [itemId]="selected?.ID"
                                [method]="'callback'"
                                [prompt]="'Are you sure you want to delete the Analytic type [' + [selected?.Name] + ']?'"                                         
                                (onCancel)="showDelete=false;"
                            ></delete-form>
                        </div>
                    </div>                    
                    <div class="col l8 s12">                        
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

export class AdminAnalyticsComponent extends AdminBaseComponent {
    statistics: StatisticType[] = [];
    selected: StatisticType;
    showEditor: boolean = false;
    showDelete: boolean = false;
    theDeleteCallback: Function;

    constructor(private statisticService: StatisticService, protected messagesService: MessagesService, headerBreadcrumbService: HeaderBreadcrumbService, pageHeader: PageHeader) {
        super(headerBreadcrumbService, pageHeader);
        this.areaDescription = "Create various types of measurements on items throughout the system, including analytics that factor into scores.";
        this.areaName = "Analytic Types";
        this.setCommonItems();
        this.theDeleteCallback = this.deleteAnalyticType.bind(this);
    }

    ngOnInit() {
        this.getAnalytics();
    }

    getAnalytics() {
        this.isLoading = true;
        this.statisticService.getStatistics()
            .then(result => {
                this.statistics = result;
                this.isLoading = false;
                if (this.statistics.length > 0) this.selected = this.statistics[0];
            });
    }

    findAnalyticTypeIndex(id: number) {
        var index: number = -1;
        for (var analytic of this.statistics) {
            index++;
            if (analytic.ID == id) return index;
        }
    }

    deleteAnalyticType(id: number) {
        this.statisticService.deleteStatistic(id);
        this.showDelete = false;
        this.selected = this.statistics.length > 0 ? this.statistics[0] : null;
        this.statistics.splice(this.findAnalyticTypeIndex(id), 1);
    }

    savePolicyType(event) {
        this.statisticService.saveStatistic(event.item)
            .then(result => {
                if (event.item.ID == undefined) {
                    event.item.ID = Number(result.id);
                    this.statistics[this.statistics.length] = event.item;
                }
                else {
                    this.statistics[this.findAnalyticTypeIndex(event.item.ID)] = event.item;
                }
                this.selected = event.item;
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