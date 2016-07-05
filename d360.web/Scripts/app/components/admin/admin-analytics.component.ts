///<reference path="../../es6-shim.d.ts"/>
import { Component} from '@angular/core';
import {DataTable, Column} from 'primeng/primeng';
import { MessagesService, HeaderBreadcrumbService, PageHeader, AnalyticsService  } from '../../services/index';
import { AdminBaseComponent } from './admin-base.component';
import { TileActionsComponent } from '../tiles/tile-actions.component';
import { Analytic } from '../../models/analytic.model';
import { DynamicEditorComponent } from '../shared/dynamic-editor.component';


@Component({
    selector: 'd3s-admin-analytics-component',
    directives: [DataTable, Column, TileActionsComponent],
    providers: [AnalyticsService],
    template: `<div class="row">
                    <div class="col l4 s12">                    
                        <div class="tile tile-detail">
                            <header *ngIf="!showEditor">Rule Types</header>  
                            <div *ngIf="isLoading">
                                <div style="padding:10px;text-align:center;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                            </div>                          
                            <p-dataTable *ngIf="!isLoading && !showEditor" [value]="analytics" selectionMode="single" [rows]="20" [paginator]="true" [pageLinks]="3" expandableRows="true" [(selection)]="selected"  (onRowDblclick)="selected=$event.data;showEditor=true;" >                                                                                        
                                <p-column field="Name" header="Name" [sortable]="true" [filter]="true"></p-column>                                                        
                            </p-dataTable>                                
                        </div>
                    </div>                    
                    <div class="col l8 s12">                        
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">           
                                    <d3s-people-responsibilities-tile [objectType]="'RuleType'" [objectID]="selected?.ID" [showHidden]="true"></d3s-people-responsibilities-tile>                        
                                </div>
                            </div>
                        </div>
                    <div>
                </div>  
                `
})

export class AdminAnalyticsComponent extends AdminBaseComponent {
    analytics: Analytic[] = [];
    selected: Analytic;
    showEditor: boolean = false;

    constructor(private analyticsService: AnalyticsService, protected messagesService: MessagesService, headerBreadcrumbService: HeaderBreadcrumbService, pageHeader: PageHeader) {
        super(headerBreadcrumbService, pageHeader);
        this.areaDescription = "Create various types of measurements on items throughout the system, including analytics that factor into scores.";
        this.areaName = "Analytic Types";
        this.setCommonItems();
    }

    ngOnInit() {
        this.getAnalytics();
    }

    getAnalytics() {
        this.isLoading = true;
        this.analyticsService.getAnalytics()
            .then(result => {
                this.analytics = result;
                this.isLoading = false;
                if (this.analytics.length > 0) this.selected = this.analytics[0];
            });
    }

}