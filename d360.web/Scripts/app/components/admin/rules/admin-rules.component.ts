import { Component, OnInit, OnDestroy } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../../services/right-sidebar.service';
import { RulesService } from '../../../services/rules.service';
import { StateService } from '../../../services/state.service';
import { AdminBaseComponent } from '../admin-base.component';
import { RuleType } from '../../../models/rule.model';
import { Title } from '@angular/platform-browser';
import { RightSidebarItem } from '../../../models/rightsidebar.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';

@Component({
    selector: 'd3s-admin-rules-component',
    providers: [RulesService],
    template: `
                <div class="row">
                    <div class="col l4 s12">                    
                        <div class="tile tile-detail">
                            <header *ngIf="!showEditor && !showDelete">Rule Types
                                <d3s-tile-actions [hasAdd]="true" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter" (addClick)="add()"></d3s-tile-actions>                            
                            </header>
                            <d3s-loading [isLoading]="isLoading"></d3s-loading>     
                            <span *ngIf="!isLoading && !showEditor && !showDelete">
                                <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..." class="grid-simple-filter">
                                <p-table #dt [value]="ruleTypes" selectionMode="single" [metaKeySelection]="true" [globalFilterFields]="['Name']" sortField="Name" [sortOrder]="1" [pageLinks]="3" [paginator]="true" [rows]="20" [(selection)]="selected">
                                    <ng-template pTemplate="header">
                                        <tr>
                                            <th [pSortableColumn]="'Name'">
                                                Name
                                                <d3s-sortIcon [field]="'Name'"></d3s-sortIcon>
                                            </th>
                                            <th style="width: 40px"></th>
                                            <th style="width: 40px"></th>
                                        </tr>
                                        <tr [hidden]="showSimpleFilter">
                                            <th><d3s-column-filter [field]="'Name'" [datatype]="'text'"></d3s-column-filter></th>
                                            <th></th>
                                            <th></th>
                                        </tr>
                                    </ng-template>
                                    <ng-template pTemplate="body" let-item>
                                        <tr (dblclick)="selected=item;showEditor=true;" [pSelectableRow]="item">
                                            <td>{{item.Name}}</td>
                                            <td>
                                                <div class="RowTools">
                                                    <a style="cursor:pointer;" (click)="selected=item;showEditor=true"><i class="fa fa-pencil"></i></a>
                                                </div>
                                            </td>
                                            <td>
                                                <div class="RowTools">
                                                    <a style="cursor:pointer;" (click)="selected=item;showDelete=true"><i class="fa fa-trash-o"></i></a>
                                                </div>
                                            </td>
                                        </tr>
                                    </ng-template>
                                    <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                                        <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                                    </ng-template>
                                </p-table>
                            </span>
                            <d3s-dynamic-editor *ngIf="showEditor" [objectID]="selected?.ID" [objectType]="'RuleType'" [title]="'Rule Type'" [selection]="selected" (saveClick)="saveRuleType($event)" (closeClick)="closeEditor()"></d3s-dynamic-editor>     
                            <d3s-delete-form *ngIf="showDelete"
                                [callback]="theDeleteCallback"
                                [itemId]="selected?.ID"
                                [method]="'callback'"
                                [prompt]="'Are you sure you want to delete the rule type [' + [selected?.Name] + ']?'"                                         
                                (onCancel)="showDelete=false;"
                            ></d3s-delete-form>
                        </div>
                    </div>                    
                    <div class="col l8 s12" *ngIf="!showEditor && !showDelete && selected">
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">                                              
                                    <object-detail [objectType]="'RuleType'" [objectID]="selected?.ID"></object-detail>
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">                                              
                                    <d3s-field-definition-tile [objectType]="'RuleType'" [objectID]="selected?.ID" ></d3s-field-definition-tile>     
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">
                                    <d3s-responsibility-relations queryType="A" [id]="selected?.AssetTypeID" [showAddButton]="false"></d3s-responsibility-relations>
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
    showEditor: boolean = false;
    showDelete: boolean = false; 
    theDeleteCallback: Function;
    private isDimensionsVisible: boolean = false;
    
    constructor(private stateService: StateService, protected rightSidebarService: RightSidebarService,
        private rulesService: RulesService,
        protected messagesService: MessagesObservableService,
        headerBreadcrumbService: HeaderBreadcrumbService,        
        titleService: Title)
    {
        super(headerBreadcrumbService, titleService, rightSidebarService);        
        this.areaName = "Rule Types";
        this.setCommonItems();
        this.theDeleteCallback = this.deleteRuleType.bind(this);
        this.setCommonRightSideBar(false, false, false);
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

    deleteRuleType(id: number) {
        this.rulesService.deleteRuleType(id)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                this.showDelete = false;
                if (result.type != 'error') {
                    this.selected = this.ruleTypes.length > 0 ? this.ruleTypes[0] : null;
                    this.ruleTypes = this.ruleTypes.filter(x => x.ID != id);
                }
                this.stateService.reloadLeftNavMenu();
            });
    }

    saveRuleType(event) {

        this.rulesService.saveRuleType(event.item)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                if (result.type != 'error') {
                    this.getRuleTypes();
                    this.showEditor = false;
                    this.stateService.reloadLeftNavMenu();
                }
            });
    }

    closeEditor() {
        this.showEditor = false;
        if (this.selected == null) {
            this.selected = this.ruleTypes.length > 0 ? this.ruleTypes[0] : null;
        }
    }

    add() {
        this.showEditor = true;
        this.selected = null;
    }

    protected showHideBreadcrumbItem(activatedItem: RightSidebarItem) {
        if (activatedItem.tag == 'dimensions') this.isDimensionsVisible = !this.isDimensionsVisible;
    }
    
}