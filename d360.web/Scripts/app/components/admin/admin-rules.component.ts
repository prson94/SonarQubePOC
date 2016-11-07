import { Component, OnInit, OnDestroy } from '@angular/core';
import { MessagesService, HeaderBreadcrumbService, RulesService, RightSidebarService  } from '../../services/index';
import {AdminBaseComponent } from './admin-base.component';
import { RuleType } from '../../models/rule.model';
import { Title } from '@angular/platform-browser';
import { RightSidebarItem } from '../../models/rightsidebar.model';

@Component({
    selector: 'd3s-admin-rules-component',
    providers: [RulesService],
    template: `
                <div *ngIf="isDimensionsVisible" class="row">
                    <div class="col s12">
                        <div class="tile tile-detail">
                            <d3s-admin-rule-dimensions></d3s-admin-rule-dimensions>
                        </div>
                    </div>
                </div>
                <div class="row" *ngIf="!isDimensionsVisible">
                    <div class="col l4 s12">                    
                        <div class="tile tile-detail">
                            <header>Rule Types</header>  
                            <d3s-loading [isLoading]="isLoading"></d3s-loading>     
                            <span *ngIf="!isLoading">
                                <input #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
                                <p-dataTable sortField="Name" [sortOrder]="1" [globalFilter]="gb" [value]="ruleTypes" selectionMode="single" [rows]="20" [paginator]="true" [pageLinks]="3" [(selection)]="selected">                                                                                        
                                    <p-column field="Name" header="Name" [sortable]="true"></p-column>                                                        
                                </p-dataTable>                                
                            </span>
                        </div>
                    </div>                    
                    <div class="col l8 s12" *ngIf="selected">                        
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">                                              
                                    <d3s-claims-tile [objectType]="'RuleType'" [objectID]="selected?.ID" [readonly]="false"></d3s-claims-tile>                 
                                </div>
                            </div>
                        </div>
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

export class AdminRulesComponent extends AdminBaseComponent implements OnInit, OnDestroy {
    ruleTypes: RuleType[] = [];
    selected: RuleType;    
    private isDimensionsVisible: boolean = false;
    
    constructor(protected rightSidebarService: RightSidebarService,
        private rulesService: RulesService,
        protected messagesService: MessagesService,
        headerBreadcrumbService: HeaderBreadcrumbService,        
        titleService: Title)
    {
        super(headerBreadcrumbService, titleService, rightSidebarService);        
        this.areaName = "Rule Types";
        this.setCommonItems();
        this.setCommonRightSideBar(false, false, false);
        this.rightSidebarService.showItem(new RightSidebarItem('Dimensions', 'dimensions'));
    }

    ngOnInit() {
        
        this.getRuleTypes();
    }

    ngOnDestroy() {        
        this.clearSidebar();
    }

    protected getRuleTypes() {
        this.isLoading = true;
        this.rulesService.getRuleTypes()
            .then(result => {
                this.ruleTypes = result;
                this.isLoading = false;
                if (this.ruleTypes.length > 0) this.selected = this.ruleTypes[0];
            });
    }  

    protected showHideBreadcrumbItem(activatedItem: RightSidebarItem) {
        if (activatedItem.tag == 'dimensions') this.isDimensionsVisible = !this.isDimensionsVisible;
    }
    
}