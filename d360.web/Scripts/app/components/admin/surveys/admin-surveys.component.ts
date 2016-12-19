import { Component } from '@angular/core';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SurveysService } from '../../../services/surveys.service';
import { MessagesService } from '../../../services/messages.service';
import { AdminBaseComponent } from '../admin-base.component'
import { Title } from '@angular/platform-browser';
import { SurveyType } from '../../../models/survey.model';

@Component({
    selector: 'd3s-admin-surveys',
    template: `                 
                <div class="row">
                    <div class="col l4 s12">                    
                        <div class="tile tile-detail">
                            <header *ngIf="!showEditor && !showDelete">Surveys
                            <d3s-tile-actions [hasAdd]="true" (addClick)="add()" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
                            </header>
                            <d3s-loading [isLoading]="isLoading"></d3s-loading>
                            <span *ngIf="!isLoading && !showDelete && !showEditor">
                                <input #gb [hidden]="!showSimpleFilter" type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
                                <p-dataTable #dt sortField="Name" [sortOrder]="1" [globalFilter]="gb" [value]="surveys" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" (onRowDblclick)="selected=$event.data;showEditor=true" [(selection)]="selected" >                                                                        
                                    <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                                    <p-column field="Name" header="Name" [sortable]="true" [style]="{width:'25%'}" [filter]="!showSimpleFilter"></p-column>                                                                                        
                                    <p-column field="ValidForDays" header="Valid Days" [sortable]="true" [style]="{width:'10%'}" [filter]="!showSimpleFilter"></p-column>
                                    <p-column [style]="{width:'60px'}">
                                        <template let-survey="rowData" pTemplate type="body">
                                            <div class="RowTools">
                                                <a style="cursor:pointer;" (click)="selected=survey;showEditor=true"><i class="fa fa-pencil"></i></a>                                        
                                                <a style="cursor:pointer;" (click)="selected=survey;showDelete=true"><i class="fa fa-trash-o"></i></a>                                    
                                            </div>
                                        </template>
                                    </p-column>                                                                                    
                                </p-dataTable>  
                            </span>
                            <d3s-dynamic-editor *ngIf="showEditor" [objectID]="selected?.ID" [objectType]="'SurveyType'" [title]="'Survey'" [selection]="selected" (saveClick)="saveSurvey($event)" (closeClick)="closeEditor()"></d3s-dynamic-editor>                        
                            <d3s-delete-form *ngIf="showDelete"
                                [callback]="theDeleteCallback"
                                [itemId]="selected?.ID"
                                [method]="'callback'"
                                [prompt]="'Are you sure you want to delete the survey [' + [selected?.Name] + ']?'"                                         
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
    

    public theDeleteCallback: Function;

    constructor(private surveysService: SurveysService, headerBreadcrumbService: HeaderBreadcrumbService, private messagesService: MessagesService, titleService: Title) {
        super(headerBreadcrumbService,  titleService);        
        this.areaName = "Surveys";
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
            .then(res => {
                this.surveys = res;
                if (this.surveys.length > 0) this.selected = this.surveys[0];
                this.isLoading = false;
            })
            .catch(error => this.error = error); // TODO: Display error message
    }

    deleteSurveyType(id: number) {
        this.surveysService.deleteSurveyTypeById(id).
            then(result => {
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
            .then(result => {
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

};
