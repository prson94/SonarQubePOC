import { Component, OnInit, OnDestroy } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { RulesService } from '../../../services/rules.service';
import { StateService } from '../../../services/state.service';
import { AdminBaseComponent } from '../admin-base.component';
import { RuleType } from '../../../models/rule.model';
import { Title } from '@angular/platform-browser';
import { SecondaryNavItem } from '../../../models/secondaryNav.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { AssetTypeClass } from '../../../models/asset.model';
import { StringConstants } from '../../../static/string-constants';
import { AssetTypeService } from '../../../services/asset-type.service';
import { AssetService } from '../../../services/asset.service';

@Component({
    selector: 'd3s-admin-rules-component',
    providers: [RulesService, AssetTypeService, AssetService],
    template: `
<div class="row">
    <div class="col l4 s12">
        <div class="tile tile-detail">
            <header *ngIf="!showDelete">
                Rule Types
                <d3s-tile-actions [hasAdd]="true" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter" (addClick)="add()"></d3s-tile-actions>
            </header>
            <d3s-loading [isLoading]="isLoading"></d3s-loading>
            <span *ngIf="!isLoading && !showDelete">
                <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..." class="grid-simple-filter">
                <p-table #dt [value]="ruleTypes" selectionMode="single" [metaKeySelection]="true" [globalFilterFields]="['Name']" sortField="Name" [sortOrder]="1" [pageLinks]="3" [paginator]="true" [rows]="20" [(selection)]="selected" (onRowSelect)="selectedItemChange(selected?.ID)">
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

          

            <d3s-delete-form *ngIf="showDelete"
                             [callback]="theDeleteCallback"
                             [itemId]="selected?.ID"
                             [method]="'callback'"
                             [prompt]="'Are you sure you want to delete the rule type [' + [selected?.Name] + ']?'"
                             (onCancel)="showDelete=false;"></d3s-delete-form>
        </div>
    </div>
    <div class="col l8 s12" *ngIf="!showEditor && !showDelete && selected">
        <div class="row">
            <div class="col s12">
                <div class="tile tile-detail">
                    <object-detail [objectType]="'RuleType'" [objectUID]="selected?.uid" [objectID]="selected?.ID"></object-detail>
                </div>
            </div>
        </div>
        <div class="row">
            <div class="col s12">
                <div class="tile tile-detail">
                    <d3s-field-definition-tile [objectType]="'RuleType'"
                                               [objectName]="selected?.Name" 
                                            [supportsPrimaryFilterOption]="true"
                                            [showAddToSearch]="true"
                                            [objectID]="selected?.ID" [assetTypeUid]="selected?.uid"></d3s-field-definition-tile>
                </div>
            </div>
        </div>
        <div class="row">
            <div class="col s12">
                <div class="tile tile-detail">
                    <d3s-responsibility-relations queryType="A" [id]="selected?.uid" [showAddButton]="false"></d3s-responsibility-relations>
                </div>
            </div>
        </div>
        <div>
        </div>

</div>
    <div class="col l8 s12" *ngIf="showEditor">
        <div class="row">
            <div class="col s12">
                <div class="tile tile-detail">
                    <d3s-asset-type-editor *ngIf="showEditor" [assetTypeClass]="assetTypeClass" [id]="selected?.AssetTypeID"
                                           [title]="(selected == null ? 'New' : 'Edit') + ' Rule Type'"
                                           (onCancel)="closeEditor()"
                                           (onComplete)="saveRuleType($event)"></d3s-asset-type-editor>
                </div>
            </div>
        </div>
    </div>
  </div>

`
})

export class AdminRulesComponent extends AdminBaseComponent implements OnInit, OnDestroy {
    ruleTypes: RuleType[] = [];
    selected: RuleType;
    showEditor: boolean = false;
    showDelete: boolean = false;
    assetTypeClass: AssetTypeClass;
    theDeleteCallback: Function;
    private isDimensionsVisible: boolean = false;

    constructor(private stateService: StateService, protected secondaryNavService: SecondaryNavService,
        private rulesService: RulesService,
        protected messagesService: MessagesObservableService,
        private assetTypeService: AssetTypeService,
        private assetsService: AssetService,
        headerBreadcrumbService: HeaderBreadcrumbService,        
        titleService: Title)
    {
        super(headerBreadcrumbService, titleService, secondaryNavService);        
        this.areaName = "Rules";
        this.setCommonItems();
        this.theDeleteCallback = this.deleteRuleType.bind(this);
    }

    ngOnInit() {
        this.assetTypeClass = AssetTypeClass.Rule;
        this.getRuleTypes();        
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    protected getRuleTypes() {
        this.isLoading = true;
        this.rulesService.getRuleTypes()
            .subscribe(result => {
                this.ruleTypes = result;
                this.isLoading = false;
                if (this.ruleTypes.length > 0) {
                    this.selected = this.ruleTypes[0];
                    this.selectedItemChange(this.selected.ID);
                }
                
            });
    }

    deleteRuleType(id: number) {
        this.rulesService.deleteRuleType(id)
            .subscribe(result => {
                this.showMessageForResult(this.messagesService, result);
                this.showDelete = false;
                if (result.type != 'error') {
                    this.ruleTypes = this.ruleTypes.filter(x => x.ID != id);
                    this.selected = this.ruleTypes.length > 0 ? this.ruleTypes[0] : null;
                }
                this.stateService.reloadLeftNavMenu();
            });
    }

    saveRuleType($event) {
        this.showEditor = false;
        this.getRuleTypes();
        this.stateService.reloadLeftNavMenu();
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

    protected showHideBreadcrumbItem(activatedItem: SecondaryNavItem) {
        if (activatedItem.tag == 'dimensions') this.isDimensionsVisible = !this.isDimensionsVisible;
    }

    selectedItemChange(objectId: number) {  
        this.loadDataAndExecuteAction();
        this.buildSecondaryNavigationForObject(objectId ? objectId : 0, StringConstants.ObjectRuleType, null, this.assetTypeClass);
    }

    private loadDataAndExecuteAction() {
        if (this.selected) {
            this.assetsService.getAssetTypeLegacyData(this.selected.uid)
                .subscribe(res => {
                    this.selected.ID = res.ObjectID;
                    this.selected.AssetTypeID = res.AssetTypeID;
                });
        }
    }
}