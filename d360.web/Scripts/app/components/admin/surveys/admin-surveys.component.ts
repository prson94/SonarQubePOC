import { Component } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SurveysService } from '../../../services/surveys.service';
import { AdminBaseComponent } from '../admin-base.component'
import { Title } from '@angular/platform-browser';
import { SurveyType } from '../../../models/survey.model';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { StringConstants } from '../../../static/string-constants';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-admin-surveys',
    template: `                 
                <div class="row">
                    <div class="col l4 s12">                    
                        <div class="tile tile-detail">
                            <header *ngIf="!showEditor && !showDelete"><ng-container i18n>Surveys</ng-container>
                            <d3s-tile-actions [hasAdd]="true" (addClick)="add()" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
                            </header>
                            <d3s-loading [isLoading]="isLoading"></d3s-loading>
                            <span *ngIf="!isLoading && !showDelete && !showEditor">
                                <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" i18n-placeholder placeholder="Search..." class="grid-simple-filter">
                                <p-table #dt [value]="surveys" selectionMode="single" [metaKeySelection]="true" [globalFilterFields]="['Name','ValidForDays']" sortField="Name" [sortOrder]="1" [pageLinks]="3" [paginator]="true" [rows]="10" [(selection)]="selected">
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
                            <d3s-dynamic-editor *ngIf="showEditor" [objectID]="selected?.ID" [objectType]="'SurveyType'" [title]="'Survey'" [selection]="selected" (saveClick)="saveSurvey($event)" (closeClick)="closeEditor()"></d3s-dynamic-editor>                        
                            <d3s-delete-form *ngIf="showDelete"
                                [callback]="theDeleteCallback"
                                [itemId]="selected?.ID"
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
                                    <object-detail [objectType]="'SurveyType'" [objectID]="selected?.ID"></object-detail>
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
    surveys: SurveyType[] = [];
    selected: SurveyType;

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
            .getSurveyTypes()
            .subscribe(res => {
                this.surveys = res.sort((a, b) => a.Name.localeCompare(b.Name));
                if (this.surveys.length > 0) this.selected = this.surveys[0];
                this.isLoading = false;
            }, err => { this.error = err })
    }

    deleteSurveyType(id: number) {
        this.surveysService.deleteSurveyTypeById(id).
            subscribe(result => {
                this.showMessageForResult(this.messagesService, result);
                //remove the template with this id from the grid
                if (result.type != 'error') {
                    this.surveys.splice(this.findSurveyTypeIndex(id), 1);
                    this.selected = this.surveys.length > 0 ? this.surveys[0] : null;
                }
                this.showDelete = false;
            });
    }

    findSurveyTypeIndex(id: number) {
        var index: number = -1;
        for (var survey of this.surveys) {
            index++;
            if (survey.ID == id) return index;
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
            .subscribe(result => {
                this.showMessageForResult(this.messagesService, result);
                if (event.item.ID == undefined) {
                    event.item.ID = Number(result.id);
                    this.surveys[this.surveys.length] = event.item;
                }
                else {
                    this.surveys[this.findSurveyTypeIndex(event.item.ID)] = event.item;
                }
                this.selected = event.item;
                this.showEditor = false;
            });
    }

}
