import { Input, Component, EventEmitter, Output, OnInit, OnChanges, SimpleChange, ViewChild } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { RulesService } from '../../services/rules.service';
import { BaseComponent } from '../shared/base.component';
import { LazyLoadEvent, DataTable } from 'primeng/primeng';
import { Rule, RuleImplementation, RuleImplementationPagedResults, RuleImplementationFilter } from '../../models/rule.model';
import { SortOrder } from '../../models/enums.model';
import { GridDefinition, GridColumn, GridField, GridFilterColumn, GridFilterExpression, GridRelationshipFilterExpression, GridAttributeFilterExpression } from '../../models/grid-definition.model';
import { RuleColumnFilterComponent } from './rule-column-filter.component'
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { MessagesObservableService } from '../../services/messages-observable.service';

@Component({
    selector: 'd3s-rule-implementations-grid',
    template: `
                <header>
                    Implementations
                    <d3s-tile-actions [hasExport]="true" [hasAdd]="true" (addClick)="add()" (exportClick)="doExport()" hasFilterMode="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>
                </header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div *ngIf="!isLoading && !showDelete && !showEditor">
                    <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..." class="grid-simple-filter">
                    <p-table #dt [value]="results" selectionMode="single" [metaKeySelection]="true" [globalFilterFields]="['Name']" [pageLinks]="3" [paginator]="true" [rows]="rowsPerPage" [selection]="selected" (selectionChange)="selected=$event;selectedChange.emit(selected)" [rowsPerPageOptions]="[5,10,20]">
                        <ng-template pTemplate="header">
                            <tr>
                                <th [pSortableColumn]="'Name'">
                                    Name
                                    <d3s-sortIcon [field]="'Name'"></d3s-sortIcon>
                                </th>
                                <th style="width: 40px"></th>
                                <th style="width: 40px"></th>
                                <th style="width: 40px"></th>
                                <th style="width: 40px"></th>
                            </tr>
                            <tr [hidden]="showSimpleFilter">
                                <th><d3s-column-filter [field]="'Name'" [datatype]="'text'"></d3s-column-filter></th>
                                <th></th>
                                <th></th>
                                <th></th>
                                <th></th>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="body" let-item>
                            <tr (dblclick)="selected=item;showRuleImplementation(selected);" [pSelectableRow]="item">
                                <td>
                                        <a (click)="showRuleImplementation(item);">{{item.Name}}</a>
                                </td>
                                <td>
                                        <div class="RowTools">
                                            <a style="cursor:pointer;" (click)="copyAs($event, item)"><i class="fa fa-copy" title="Copy"></i></a>
                                        </div>
                                </td>
                                <td>
                                        <div class="RowTools">
                                            <a *ngIf="item.SourceUri" [href]="item.SourceUri"><i class="fa fa-info" title="Source Uri"></i></a>
                                        </div>
                                </td>
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
                        <ng-template pTemplate="summary">
                            <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                        </ng-template>
                    </p-table>
                </div>  
                <d3s-dynamic-editor *ngIf="showEditor" [objectID]="selected?.ID" [copy]="copy" [objectType]="'RuleImplementation'" [title]="'Rule Implementation'" [selection]="selected" (saveClick)="saveImplementation($event)" (closeClick)="closeEditor()"></d3s-dynamic-editor>     
                <d3s-delete-form *ngIf="showDelete"
                    [callback]="theDeleteCallback"
                    [itemId]="selected?.ID"
                    [method]="'callback'"
                    [prompt]="'Are you sure you want to delete the rule implementation [' + [selected?.Name] + ']?'"                                         
                    (onCancel)="showDelete=false;"
                ></d3s-delete-form> 
                `,
    providers: [RulesService],
})

export class RuleImplementationsGridComponent extends BaseComponent implements OnInit {

    @Input() ruleId: number;
    
    @Input() selected: RuleImplementation;
    @Output() selectedChange = new EventEmitter();
    private rowsPerPage: number = 10;
    private results: RuleImplementation[];
    columns: GridColumn[] = [];
    fields: GridField[] = [];
    filtercolumns: GridFilterColumn[] = [];

    @ViewChild(RuleColumnFilterComponent) private filtersComponent: RuleColumnFilterComponent;

    currentPageNumber: number = 0;
    sortField: string = "";
    sortOrder: SortOrder = SortOrder.None;
    filters: GridFilterExpression[] = [];
    relationships: GridRelationshipFilterExpression;
    attributes: GridAttributeFilterExpression;
    copy: boolean;
    searchValue: string = "";
    simpleSearchID: number = 0;
    searchDelayMilliSeconds: number = 300;
    theDeleteCallback: Function;

    private showDelete: boolean = false;
    private showEditor: boolean = false;
    
    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private ruleService: RulesService,
        private messagesService: MessagesObservableService) {
        super();
        this.theDeleteCallback = this.deleteImplementation.bind(this);
    }

    ngOnInit() {
       
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes['ruleId'] && this.ruleId) {
            this.filters = [];
            this.getData();
        }
    }
    
    public filterGridData(filterData) {     
        this.currentPageNumber = 0;        
        this.getData();
    }

    private getData() {
        if (!this.ruleId) {
            console.log("ERROR - NO RULE ID");
            return;
        }

        //remove any invalid filters
        if (this.filters && this.filters.length > 0) {
            for (var i = this.filters.length - 1; i >= 0; i--) {
                if (!this.filters[i].field || !this.filters[i].value) {                    
                    this.filters.splice(i, 1);
                }
            }
        }

        this.ruleService.getRuleImplementations(this.ruleId)//, this.currentPageNumber, this.rowsPerPage, this.sortField, this.sortOrder, this.filters, this.simpleTextFilter)
            .then(res => {
                this.results = res;
                if (this.results && this.results.length > 0) {
                    this.selected = this.results[0];
                    this.selectedChange.emit(this.selected);
                }
            });

    }

    private deleteImplementation(id: number) {
        this.ruleService.deleteRuleImplementation(id).
            then(result => {
                this.showMessageForResult(this.messagesService, result);
                this.showDelete = false;
                this.results = this.results.filter(x => x.ID != id);
            });
    }

    private checkSimpleSearchEnter(event, dt: DataTable) {
        if (event.keyCode == 13) this.doSimpleSearch(dt);
        else {
            if (this.simpleSearchID > 0) {
                window.clearTimeout(this.simpleSearchID);
                this.simpleSearchID = 0;
            }

            this.simpleSearchID = window.setTimeout(() => this.doSimpleSearch(dt), this.searchDelayMilliSeconds);

        }
    }

    private doSimpleSearch(dt: DataTable) {
        if (dt) dt.reset();
        this.getData();
    }

    private doExport() {
        this.ruleService.getResultsByRuleExcel(this.ruleId);
    }

    resetFilters() {
        this.filtersComponent.resetFilters();
    }

    private showRuleImplementation(impl) {
        this.router.navigateByUrl(SiteUrlHelpers.getDeepObjectUrl('ruleimplementation', impl.RuleTypeID, impl.RuleID, impl.ID));
    }

    private add() {
        this.selected = null;
        this.showEditor = true;
    }
    private copyAs(event, item) {
        this.copy = true;
        this.showEditor = true;
    }
    private saveImplementation(event) {       
        event.item.RuleID = this.ruleId;
        this.ruleService.saveRuleImplementation(event.item, event.action)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                this.getData();
                this.copy = false;
                this.showEditor = false;
            });
    }

    private closeEditor() {
        this.showEditor = false;
        if (this.selected == null && this.results.length > 0)
            this.selected = this.results[0];
    }
};