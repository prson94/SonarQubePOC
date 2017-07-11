import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { GridDefinition, GridColumn, GridField, GridFilterColumn, GridFilterExpression, GridRelationshipFilterExpression, GridAttributeFilterExpression } from '../../models/grid-definition.model';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { RulesService } from '../../services/rules.service';
import { GridDefinitionService } from '../../services/grid-definition.service';
import { MessagesService } from '../../services/messages.service';
import { HeaderActionsService } from '../../services/header-actions.service';
import { PermissionsService } from '../../services/permissions.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { RuleDimension, Rule, RuleType, RuleClassification, RuleStatus } from '../../models/rule.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { StringConstants } from '../../static/string-constants';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-rule-list',
    providers: [GridDefinitionService, RulesService, PermissionsService],
    template: ` 
                <div class="row">
                    <div class="col s12">
                        <d3s-loading [isLoading]="isLoading"></d3s-loading>
                        <div class="tile tile-detail" >    
                            <div class="row" *ngIf="!isLoading && !showDelete && !showEditor">                        
                                <div class="col s12">
                                    <header>{{modelGroup}} Rules                                
                                        <d3s-tile-actions [hasAdd]="hasRootCreatePermissions()" (addClick)="showAddRule()" hasFilterMode="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                                                     
                                    </header>
                                    <input #gb [hidden]="!showSimpleFilter" type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">                                                                                     
                                    <p-dataTable #dt sortField="Name" [sortOrder]="1" [globalFilter]="gb" [value]="rules" selectionMode="single" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions" paginator="true" pageLinks="3" [(selection)]="selected"  (onRowDblclick)="selected=$event.data;showRule(selected);" >                                        
                                        <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>
                                        <p-column field="ID" header="ID" sortable="true" [style]="{width:'5%'}" [filter]="!showSimpleFilter"></p-column>
                                        <p-column field="Name" header="Name" sortable="true" [style]="{width:'45%'}" [filter]="!showSimpleFilter">
                                            <ng-template let-item="rowData" pTemplate type="body">
                                                <a (click)="showRule(item)">{{item?.Name}}</a>
                                            </ng-template>
                                        </p-column>
                                        <p-column field="StatusName" header="Status" sortable="true" [filter]="!showSimpleFilter" [style]="{width:'15%'}"></p-column>
                                        <p-column field="Dimension" header="Dimension" sortable="custom" (sortFunction)="columnDimSort($event)" [style]="{width:'20%'}" [filter]="!showSimpleFilter"></p-column>                                        
                                        <p-column *ngFor="let column of columns" [field]="column.datafield" [header]="column.text" [sortable]="column.sortable" [filter]="!showSimpleFilter">                                                                
                                            <ng-template let-item="rowData" pTemplate type="body">
                                                <d3s-dynamic-field-value [column]="column" [fields]="fields" [item]="item"></d3s-dynamic-field-value>                                 
                                            </ng-template>
                                        </p-column>
                                        <p-column [style]="{width:'40px'}" *ngIf="hasRootUpdatePermissions()">
                                            <ng-template let-item="rowData" pTemplate type="body">
                                                <div class="RowTools">
                                                    <a style="cursor:pointer;" (click)="selected=item;showEditor=true;"><i class="fa fa-pencil"></i></a>                                        
                                                </div>
                                            </ng-template>
                                        </p-column>                            
                                        <p-column  [style]="{width:'40px'}" *ngIf="hasRootDeletePermissions()">
                                                <ng-template let-item="rowData" pTemplate type="body">
                                                    <div class="RowTools">                                
                                                        <a style="cursor:pointer;" (click)="selected=item;showDelete=true;"><i class="fa fa-trash-o"></i></a>                                    
                                                    </div>
                                                </ng-template>
                                        </p-column> 
                                    </p-dataTable>      
                                </div>
                            </div>
                            <d3s-dynamic-editor *ngIf="showEditor" [objectID]="ruleType?.ID" [objectType]="'Rule'" [title]="'Rule'" [selection]="selected" (saveClick)="saveRule($event)" (closeClick)="showEditor = false;"></d3s-dynamic-editor>
                            <d3s-delete-form *ngIf="showDelete"
                                                    [callback]="theDeleteCallback"
                                                    [itemId]="selected?.ID"
                                                    [method]="'callback'"
                                                    [prompt]="'Are you sure you want to delete the selected item?'"                                         
                                                    (onCancel)="showDelete=false;"
                            ></d3s-delete-form>  
                        </div>                        
                    </div>
                </div>
                `
})

export class RuleListComponent extends BaseComponent implements OnInit, OnDestroy {
    sub: any;
    ruleTypeId: number;
    private rules: any[] = [];
    private selected: Rule;
    private ruleType: RuleType;
    private showEditor: boolean = false;
    private showDelete: boolean = false;

    columns: GridColumn[] = [];
    fields: GridField[] = [];
    filtercolumns: GridFilterColumn[] = [];

    theDeleteCallback: Function;
    
    constructor(private route: ActivatedRoute,
        private router: Router,
        protected rulesService: RulesService,
        protected titleService: Title,
        protected messagesService: MessagesService,
        private gridDefinitionService: GridDefinitionService, 
        private headerActionsService: HeaderActionsService,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected permissionsService: PermissionsService
    ) {
        super();

        this.theDeleteCallback = this.deleteRule.bind(this);
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {

            this.ruleTypeId = +params['ruleTypeId'];
            this.headerBreadcrumbService.setCurrentObjectInfo('RuleType', this.ruleTypeId);

            this.loadPermissions(this.permissionsService, StringConstants.ObjectRuleType, this.ruleTypeId);

            this.getFieldsDefinition();

            this.isLoading = true;
            this.rulesService.getRuleType(this.ruleTypeId)
                .then(result => {
                    this.isLoading = false;
                    this.ruleType = result;
                    this.headerBreadcrumbService.clearBreadcrumbs();
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Rules', undefined)); //`${SiteUrlHelpers.SITE_URL_RULE_ROOT}`
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.ruleType.Name, `${SiteUrlHelpers.SITE_URL_RULE_ROOT}/${this.ruleTypeId}`));

                    this.loadPermissions(this.permissionsService, StringConstants.ObjectRuleType, this.ruleTypeId);

                    this.loadRules();

                    this.setBrowserTitle(this.titleService, this.ruleType.Name);
                });
        });
    }

    ngOnDestroy() {
        this.clearSidebar();
        this.sub.unsubscribe();
    }

    getFieldsDefinition() {
        this.gridDefinitionService.getGridDefinition(this.ruleTypeId, StringConstants.ObjectRuleType)
            .then(result => {
                this.columns = result.Columns.filter(x => x.datafield != 'Name');
                this.filtercolumns = result.FilterColumns;
                this.fields = result.Fields;
            });
    }

    private loadRules() {
        this.isLoading = true;
        this.rulesService.getRules(this.ruleTypeId)
            .then(result => {
                this.isLoading = false;
                for (let rule of result) {
                    if (!rule.Dimension) rule.Dimension = ""; //prime grid has issues with null objects make sure we dont have any.
                    rule.StatusName = RuleStatus[rule.Status];
                }
                this.rules = result;   
                console.log(this.rules);  
                                              
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
                    this.headerActionsService.emitFavoritesChange();
                }
                this.showEditor = false;
            });
    }

    private showRule(rule) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('rule', rule.ID, this.ruleTypeId));
    }
        
    private deleteRule(id: number) {
        this.rulesService.deleteRule(id).then(result => {
            this.showMessageForResult(this.messagesService, result);
            this.showDelete = false;
            this.selected = this.rules.length > 0 ? this.rules[0] : null;
            this.rules = this.rules.filter(x => x.ID != id);
            this.headerActionsService.emitFavoritesChange();
        });
    }
    
    private columnDimSort(event) {
        //event.field = Field to sort
        //event.order = Sort order, 1 ascending , -1 descending                        
        this.rules = _.sortBy(this.rules, 'Dimension');
        if (event.order == -1) this.rules.reverse();
    }    
};