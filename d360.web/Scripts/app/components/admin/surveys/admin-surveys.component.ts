import { Component } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SurveysService } from '../../../services/surveys.service';
import { AdminBaseComponent } from '../admin-base.component';
import { Title } from '@angular/platform-browser';
import { SurveyTypeV2 } from '../../../models/survey.model';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { StringConstants } from '../../../static/string-constants';
import { CompanySettingsService } from '../../../services/settings.service';
import { LazyLoadEvent } from 'primeng/api';
import { SortOrder } from '../../../models/enums.model';
import { V2ApiFilters } from '../../../models/asset-search.model';
import { AdvancedFiltersHelper } from '../../../static/advanced-filter-helpers';

@Component({
    selector: 'd3s-admin-surveys',
    template: `                 
                <div class="row">
                    <div class="col l4 s12">                    
                        <div class="tile tile-detail">
                            <header *ngIf="!showEditor && !showDelete"><ng-container i18n>Surveys</ng-container>
                            <d3s-tile-actions [hasAdd]="true" (addClick)="add()" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
                            </header>
                            <span *ngIf="!showDelete && !showEditor">
                                <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" i18n-placeholder placeholder="Search..." class="grid-simple-filter">
                                <p-table #dt 
                                    [value]="surveys" 
                                    selectionMode="single"
                                    [metaKeySelection]="true" 
                                    [globalFilterFields]="['Name','ValidForDays']" 
                                    [sortField]="sortField" 
                                    [sortOrder]="sortOrder" 
                                    [pageLinks]="3" 
                                    [paginator]="true" 
                                    [rows]="rowsPerPage"
                                    [(selection)]="selected"
                                    [first]="0"
                                    [lazy]="true"
                                    (onLazyLoad)="loadSurveyTypesLazy($event)"
                                    [totalRecords]="totalRecords"
                                    [loading]="isLoading"
                                    [loadingIcon]="'fa fa-spinner fa-spin'"
                                    >                                  
                                    <ng-template pTemplate="header">
                                        <tr>
                                            <th [pSortableColumn]="'Name'" style="width: 25%">
                                                <ng-container i18n>Name</ng-container>
                                                <d3s-sortIcon [field]="'Name'"></d3s-sortIcon>
                                            </th>
                                            <th [pSortableColumn]="'ValidForDays'" style="width: 10%">
                                                <ng-container i18n>Valid Days</ng-container>
                                                <d3s-sortIcon [field]="'ValidForDays'"></d3s-sortIcon>
                                            </th>
                                            <th style="width: 60px"></th>
                                        </tr>
                                        <tr [hidden]="showSimpleFilter">
                                            <th><d3s-column-filter [field]="'Name'" [datatype]="'text'"></d3s-column-filter></th>
                                            <th><d3s-column-filter [field]="'ValidForDays'" [datatype]="'text'"></d3s-column-filter></th>
                                            <th></th>
                                        </tr>
                                    </ng-template>
                                    <ng-template pTemplate="body" let-item>
                                        <tr (dblclick)="selected=item;showEditor=true" [pSelectableRow]="item">
                                            <td>{{item.Name}}</td>
                                            <td>{{item.ValidForDays}}</td>
                                            <td>
                                                <div class="RowTools">
                                                    <a style="cursor:pointer;" (click)="selected=item;showEditor=true"><i class="fa fa-pencil"></i></a>
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
                            <d3s-dynamic-editor *ngIf="showEditor" [useObjectUidForDefinition]="true" [objectUid]="selected?.Uid" [objectType]="'SurveyType'" [title]="'Survey'" [selection]="selected" (saveClick)="saveSurvey($event)" (closeClick)="closeEditor()"></d3s-dynamic-editor>                        
                            <d3s-delete-form *ngIf="showDelete"
                                [callback]="theDeleteCallback"
                                [itemId]="selected?.Uid"
                                [method]="'callback'"
                                [prompt]="deletePromptText"                                         
                                (onCancel)="showDelete=false;"
                            ></d3s-delete-form>   
                    </div>
                </div>  
                <div class="col l8 s12" *ngIf="!showEditor && !showDelete && selected">
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">                                              
                                    <object-detail [objectType]="'SurveyType'" [objectUID]="selected?.Uid"></object-detail>
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">                                              
                                    <d3s-admin-survey-questions [survey]="selected"></d3s-admin-survey-questions>
                                </div>
                            </div>
                        </div>
                    <div>             
               </div>               
                `,
    providers: [SurveysService],
})

export class AdminSurveysComponent extends AdminBaseComponent {
    surveys: SurveyTypeV2[] = [];
    selected: SurveyTypeV2;

    pageNum = 0;
    rowsPerPage = 10;
    sortOrder: number = 1;
    sortField: string = 'Name';
    simpleTextFilter: string = '';
    filters: LazyLoadEvent['filters'] = {};
    totalRecords: number;

    error: any;

    showDelete: boolean = false;
    showEditor: boolean = false;

    get deletePromptText(): string {
        return $localize`Are you sure you want to delete the survey [${this.selected?.Name}]?`;
    }

    public theDeleteCallback: Function;

    constructor(private surveysService: SurveysService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        private messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        titleService: Title,
        secondaryNavService: SecondaryNavService) {
        super(headerBreadcrumbService, titleService, settingsService, secondaryNavService);
        this.areaName = StringConstants.Section_Surveys;
        this.setCommonItems();
    }

    ngOnInit() {
        this.getTemplates();
        this.theDeleteCallback = this.deleteSurveyType.bind(this);
    }

    getTemplates() {
        this.isLoading = true;
        this.surveysService
            .getSurveyTypes(this.getSurveyTypesParams())
            .subscribe((res) => {
                this.totalRecords = res.total;
                this.surveys = res.items.sort((a, b) => a.Name.localeCompare(b.Name));
                if (this.surveys.length > 0) this.selected = this.surveys[0];
                this.isLoading = false;
            }, (err) => { this.error = err; });
    }
    
    getSurveyTypesParams() {
        const params = new V2ApiFilters();
        params._pageNum = this.pageNum + 1;

        params._pageSize = this.rowsPerPage;
        if (this.sortField) {
            params._order = this.sortField;
        }

        if (this.sortOrder !== SortOrder.None) {
            params._direction = this.sortOrder === SortOrder.Ascending ? "asc" : "desc";
        }

        if (this.simpleTextFilter && this.simpleTextFilter.length > 0) {
            params._simpleFilter = encodeURIComponent(this.simpleTextFilter);
        }
        
        const advancedFilter = AdvancedFiltersHelper.parseFiltersFromTableFilters(this.filters, [
            {
                apiName: 'Name',
                fieldType: 'text',
                name: 'Name',
                type: 'text'
            },
            {
                apiName: 'ValidForDays',
                fieldType: 'number',
                name: 'ValidForDays',
                type: 'number'
            }
        ]);

        if (advancedFilter.length > 0) {
            params['_filter'] = advancedFilter;
        }

        return params;
    }

    loadSurveyTypesLazy(event: LazyLoadEvent) {
        this.pageNum = event.first / event.rows;
        this.sortOrder = event.sortOrder;
        this.sortField = event.sortField;
        this.rowsPerPage = event.rows;
        this.simpleTextFilter = event.globalFilter;
        this.filters = event.filters;
        this.getTemplates();
    }

    deleteSurveyType(uid: string) {
        this.surveysService.deleteSurveyTypeById(uid).
            subscribe((result) => {
                if (result !== true) {
                    // error happened
                    this.showDelete = false;
                    return;
                }
                
                this.messagesService.showInfoMessage(
                    null,
                    $localize`Success`
                );

                //remove the template with this id from the grid
                this.surveys.splice(this.findSurveyTypeIndex(uid), 1);
                this.selected = this.surveys.length > 0 ? this.surveys[0] : null;
                this.showDelete = false;
            });
    }

    findSurveyTypeIndex(uid: string) {
        var index: number = -1;
        for (var survey of this.surveys) {
            index++;
            if (survey.Uid === uid) {
                return index;
            }
        }
    }

    closeEditor() {
        this.showEditor = false;
        if (this.selected == null && this.surveys.length > 0)
            this.selected = this.surveys[0];
    }

    add() {
        this.showEditor = true;
        this.selected = null;
    }

    saveSurvey(event) {
        this.surveysService.saveSurveyType(event.item)
            .subscribe((result) => {
                if (result == null) {
                    return;
                }

                this.messagesService.showInfoMessage(
                    null,
                    $localize`Success`
                );

                if (event.item.Uid == null) {
                    event.item.Uid = result.Uid;
                    this.surveys[this.surveys.length] = event.item;
                }
                else {
                    this.surveys[this.findSurveyTypeIndex(event.item.Uid)] = event.item;
                }

                this.selected = event.item;
                this.showEditor = false;
            });
    }

}
