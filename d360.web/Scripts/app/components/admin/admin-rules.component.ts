///<reference path="../../es6-shim.d.ts"/>
import { Component} from '@angular/core';
import {DataTable, Column} from 'primeng/primeng';
import { MessagesService, HeaderBreadcrumbService, PageHeader, RulesService  } from '../../services/index';
import {AdminBaseComponent} from './admin-base.component';
import { TileActionsComponent } from '../tiles/tile-actions.component';
import { PeopleResponsibilitiesTile } from '../tiles/people-responsibilities.tile';
import { ClaimsTile } from '../tiles/claims.tile';
import { RuleDimensionsTile } from '../tiles/rule-dimensions.tile';
import { RuleType } from '../../models/rule.model';
import { DynamicEditorComponent } from '../shared/dynamic-editor.component';
import { Title } from '@angular/platform-browser';


@Component({
    selector: 'd3s-admin-rules-component',
    directives: [DataTable, Column, TileActionsComponent, PeopleResponsibilitiesTile, ClaimsTile, RuleDimensionsTile ],
    providers: [RulesService],
    template: `<div class="row">
                    <div class="col l4 s12">                    
                        <div class="tile tile-detail">
                            <header *ngIf="!showEditor">Rule Types</header>  
                            <div *ngIf="isLoading">
                                <div style="padding:10px;text-align:center;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                            </div>                          
                            <p-dataTable *ngIf="!isLoading && !showEditor" [value]="ruleTypes" selectionMode="single" [rows]="20" [paginator]="true" [pageLinks]="3" expandableRows="true" [(selection)]="selected"  (onRowDblclick)="selected=$event.data;showEditor=true;" >                                                                                        
                                <p-column field="Name" header="Name" [sortable]="true" [filter]="true"></p-column>                                                        
                            </p-dataTable>                                
                        </div>
                    </div>                    
                    <div class="col l8 s12">
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">                                              
                                    <d3s-rule-dimensions-tile></d3s-rule-dimensions-tile>
                                </div>
                            </div>
                        </div>
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

export class AdminRulesComponent extends AdminBaseComponent {
    ruleTypes: RuleType[] = [];
    selected: RuleType;
    showEditor: boolean = false;
    
    constructor(private rulesService: RulesService, protected messagesService: MessagesService, headerBreadcrumbService: HeaderBreadcrumbService, pageHeader: PageHeader, titleService: Title) {
        super(headerBreadcrumbService, pageHeader, titleService);
        this.areaDescription = "Here you can configure the properties available to rules, including what dimensions are defined.";
        this.areaName = "Rule Types";
        this.setCommonItems();
    }

    ngOnInit() {
        
        this.getRuleTypes();
    }

    getRuleTypes() {
        this.isLoading = true;
        this.rulesService.getRuleTypes()
            .then(result => {                
                this.ruleTypes = result;
                this.isLoading = false;
                if (this.ruleTypes.length > 0) this.selected = this.ruleTypes[0];
            });
    }  
    
}