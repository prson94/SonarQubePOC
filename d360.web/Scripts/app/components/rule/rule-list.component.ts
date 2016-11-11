import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService, RulesService, MessagesService } from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { RuleDimension, Rule, RuleClassification, RuleStatus } from '../../models/rule.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-rule-list',
    providers: [RulesService],
    template: ` 
                <div class="row">
                    <div class="col s12">
                        <d3s-loading [isLoading]="isLoading"></d3s-loading>
                        <div class="tile tile-detail" >    
                            <div class="row" *ngIf="!isLoading && !showDelete && !showEditor">                        
                                <div class="col s12">
                                    <header>{{modelGroup}} Rules                                
                                        <d3s-tile-actions hasAdd="true" (addClick)="showAddRule()" hasFilterMode="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                                                     
                                    </header>      
                                    <input #gb [hidden]="!showSimpleFilter" type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">                                                                                     
                                    <p-dataTable #dt sortField="Name" [sortOrder]="1" [globalFilter]="gb" [value]="rules" selectionMode="single" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions" paginator="true" pageLinks="3" [(selection)]="selected"  (onRowDblclick)="selected=$event.data;showRule(selected);" >                                        
                                        <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                                        <p-column field="Name" header="Name" sortable="custom" (sortFunction)="columnSort($event)" [style]="{width:'45%'}" [filter]="!showSimpleFilter">
                                            <template let-item="rowData" pTemplate type="body">
                                                <a (click)="showRule(item)">{{item?.Name}}</a>
                                            </template>
                                        </p-column>
                                        <p-column field="ID" header="ID" sortable="custom" (sortFunction)="columnSort($event)"  [style]="{width:'10%'}" [filter]="!showSimpleFilter"></p-column>                                                                                                                                                                                                                                                
                                        <p-column field="StatusName" header="Status" sortable="custom" [filter]="!showSimpleFilter" (sortFunction)="columnSort($event)" [style]="{width:'15%'}"></p-column>
                                        <p-column field="Dimension.Name" header="Dimension" sortable="custom" (sortFunction)="columnDimSort($event)" [style]="{width:'15%'}" [filter]="!showSimpleFilter"></p-column>                                        
                                        <p-column [style]="{width:'40px'}">
                                            <template let-item="rowData" pTemplate type="body">
                                                <div class="RowTools">
                                                    <a style="cursor:pointer;" (click)="selected=item;showEditor=true;"><i class="fa fa-pencil"></i></a>                                        
                                                </div>
                                            </template>
                                        </p-column>                            
                                        <p-column  [style]="{width:'40px'}">
                                                <template let-item="rowData" pTemplate type="body">
                                                    <div class="RowTools">                                
                                                        <a style="cursor:pointer;" (click)="selected=item;showDelete=true;"><i class="fa fa-trash-o"></i></a>                                    
                                                    </div>
                                                </template>
                                        </p-column> 
                                    </p-dataTable>      
                                </div>
                            </div>
                            <d3s-dynamic-editor *ngIf="showEditor" [objectID]="selected?.ID" [objectType]="'Rule'" [title]="'Rule'" [selection]="selected" (saveClick)="saveRule($event)" (closeClick)="showEditor = false;"></d3s-dynamic-editor>
                            <delete-form *ngIf="showDelete"
                                                    [callback]="theDeleteCallback"
                                                    [itemId]="selected?.ID"
                                                    [method]="'callback'"
                                                    [prompt]="'Are you sure you want to delete the selected item?'"                                         
                                                    (onCancel)="showDelete=false;"
                            ></delete-form>  
                        </div>                        
                    </div>
                </div>
                `
})

export class RuleListComponent extends BaseComponent implements OnInit {
    private rules: Rule[] = [];
    private selected: Rule;
    private showEditor: boolean = false;
    private showDelete: boolean = false;

    theDeleteCallback: Function;
    
    constructor(private route: ActivatedRoute,
        private router: Router,
        protected rulesService: RulesService,
        protected titleService: Title,
        protected messagesService: MessagesService,
        protected headerBreadcrumbService: HeaderBreadcrumbService) {
        super();

        this.theDeleteCallback = this.deleteRule.bind(this);
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Rules');

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Rules'));

        this.loadRules();
    }

    private loadRules() {
        this.isLoading = true;
        this.rulesService.getRules()
            .then(result => {
                this.isLoading = false;
                for (let rule of result) {
                    if (!rule.Dimension) rule.Dimension = new RuleDimension(); //prime grid has issues with null objects make sure we dont have any.
                    rule.RuleTypeName = RuleClassification[rule.RuleType];
                    rule.StatusName = RuleStatus[rule.Status];
                }
                this.rules = result;     
                                              
                if (this.rules.length && this.rules.length > 0) this.selected = this.rules[0];
            });
    }

    private showAddRule() {
        this.selected = null;
        this.showEditor = true;
    }

    private saveRule(event) {
        this.rulesService.saveRule(event.item)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                if (result.type != 'error') {
                    this.loadRules();
                }
                this.showEditor = false;
            });
    }

    private showRule(rule) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('rule', rule.ID));
    }

    
    private deleteRule(id: number) {
        this.rulesService.deleteRule(id).then(result => {
            this.showMessageForResult(this.messagesService, result);
            this.showDelete = false;
            this.selected = this.rules.length > 0 ? this.rules[0] : null;
            this.rules = this.rules.filter(x => x.ID != id);
        });
    }
    
    private columnDimSort(event) {
        //event.field = Field to sort
        //event.order = Sort order, 1 ascending , -1 descending                        
        this.rules = _.sortBy(this.rules, 'Dimension.Name');
        if (event.order == -1) this.rules.reverse();
    }

    private columnSort(event) {
        //event.field = Field to sort
        //event.order = Sort order, 1 ascending , -1 descending                        
        this.rules = _.orderBy(this.rules, [item => item[event.field] ? (item[event.field].toLowerCase ? item[event.field].toLowerCase() : item[event.field] ) : item[event.field]], [event.order == -1 ? 'desc' : 'asc']);
    }
};