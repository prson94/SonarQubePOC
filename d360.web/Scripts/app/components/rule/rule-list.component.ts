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
import { RightSidebarService } from '../../services/right-sidebar.service';
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
                                        <d3s-tile-actions [hasAdd]="hasModifyAssetPermissions()" (addClick)="showAddRule()" hasFilterMode="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                                                     
                                    </header>
                                        <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..." class="grid-simple-filter">
                                        <p-table #dt [value]="rules" selectionMode="single" [metaKeySelection]="true" [globalFilterFields]="globalFilterFields" sortField="Name" [sortOrder]="1" [pageLinks]="3" [paginator]="true" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions" [(selection)]="selected">
                                            <ng-template pTemplate="header">
                                                <tr>
                                                    <th [pSortableColumn]="'ID'" style="width: 5%">
                                                        ID
                                                        <d3s-sortIcon [field]="'ID'"></d3s-sortIcon>
                                                    </th>
                                                    <th [pSortableColumn]="'Dimension'" style="width:20%">
                                                        Dimension
                                                        <d3s-sortIcon [field]="'Dimension'"></d3s-sortIcon>
                                                    </th>
                                                     <th *ngFor="let col of columns" [pSortableColumn]="col.sortable ? col.datafield : null" >
                                                        {{col.text}}
                                                        <d3s-sortIcon *ngIf="col.sortable" [field]="col.datafield"></d3s-sortIcon>
                                                    </th>
                                                    <th style="width: 40px;"></th>
                                                    <th style="width: 40px;"></th>
                                                </tr>
                                                <tr [hidden]="showSimpleFilter">
                                                    <th><d3s-column-filter [field]="'ID'" [datatype]="'text'"></d3s-column-filter></th>
                                                    <th><d3s-column-filter [field]="'Dimension'" [datatype]="'text'"></d3s-column-filter></th>
                                                    <th *ngFor="let col of columns">
                                                          <d3s-column-filter [field]="col.datafield" [datatype]="'text'"></d3s-column-filter>
                                                      </th>
                                                    <th></th>
                                                    <th></th>
                                                </tr>
                                            </ng-template>
                                            <ng-template pTemplate="body" let-item>
                                                <tr [pSelectableRow]="item">
                                                    <td>
                                                            <a (click)="selected=item;showRule(selected);">{{item["ID"]}}</a>
                                                    </td>
                                                    <td>
                                                            <a (click)="selected=item;showRule(selected);">{{item["Dimension"]}}</a>
                                                    </td>
                                                    <td *ngFor="let column of columns">
                                                            <a (click)="selected=item;showRule(selected);"><d3s-dynamic-field-value [column]="column" [fields]="fields" [item]="item"></d3s-dynamic-field-value></a>
                                                    </td>
                                                    <td>
                                                            <div class="RowTools" *ngIf="item.P_CanEdit">
                                                                <a style="cursor:pointer;" (click)="selected=item;showEditor=true;"><i class="fa fa-pencil"></i></a>
                                                            </div>
                                                    </td>
                                                    <td>
                                                            <div class="RowTools" *ngIf="item.P_CanDelete">
                                                                <a style="cursor:pointer;" (click)="selected=item;showDelete=true;"><i class="fa fa-trash-o"></i></a>
                                                            </div>
                                                    </td>
                                                </tr>
                                            </ng-template>
                                            <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                                                <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                                            </ng-template>
                                        </p-table>

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
        protected permissionsService: PermissionsService,
        rightSidebarService: RightSidebarService
    ) {
        super();
        this.rightSidebarService = rightSidebarService;

        this.theDeleteCallback = this.deleteRule.bind(this);
    }

    get globalFilterFields(): string[] {
        let f = this.columns.map(c => c.datafield);
        f.push('ID');
        f.push('Dimension');
        return f;
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
                    this.setObjectInfo('RuleType', this.ruleType.ID);
                    this.headerBreadcrumbService.clearBreadcrumbs();
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Rules', undefined));
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.ruleType.Name, `${SiteUrlHelpers.SITE_URL_RULE_ROOT}/${this.ruleTypeId}`));

                    this.loadPermissions(this.permissionsService, StringConstants.ObjectRuleType, this.ruleTypeId);

                    this.setCommonRightSideBar(false, false, this.ruleType.HasDashboards);

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