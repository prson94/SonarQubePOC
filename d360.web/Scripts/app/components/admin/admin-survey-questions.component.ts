
import { Component, Input, OnChanges, SimpleChange} from '@angular/core';
import { SurveyQuestionType, SurveyType } from '../../models/survey.model';
import { MessagesService, SurveysService  } from '../../services/index';
import { BaseComponent } from '../shared/base.component';

@Component({
    selector: 'd3s-admin-survey-questions',
    providers: [SurveysService],
    template: `
               <header *ngIf="!showEditor && !showDelete">Questions
                <d3s-tile-actions [hasAdd]="true" (addClick)="add()" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
               </header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <span *ngIf="!isLoading && !showDelete && !showEditor">
                    <input [hidden]="showSimpleFilter" #gb type="text" pInputText size="100" placeholder="Search..." style="margin-bottom:10px;width:100%;">                    
                    <p-dataTable [globalFilter]="gb" [value]="questions" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" (onRowDblclick)="selected=$event.data;showEditor=true" [(selection)]="selected" >                                                                        
                        <p-column field="Name" header="Name" [sortable]="true" [filter]="showSimpleFilter"></p-column>                                                            
                        <p-column field="DisplayStyle" header="Display Type" [sortable]="true" [filter]="showSimpleFilter"></p-column>
                        <p-column [style]="{width:'40px'}">
                                <template let-question="rowData" pTemplate type="body">
                                    <div class="RowTools">
                                        <a style="cursor:pointer;" (click)="selected=question;showEditor=true"><i class="fa fa-pencil"></i></a>                                      
                                    </div>
                                </template>
                        </p-column>                                                
                        <p-column [style]="{width:'40px'}">
                                <template let-question="rowData" pTemplate type="body">
                                    <div class="RowTools">                                    
                                        <a style="cursor:pointer;" (click)="selected=question;showDelete=true"><i class="fa fa-trash-o"></i></a>
                                    </div>
                                </template>
                        </p-column>                                                
                    </p-dataTable>      
                </span>
                <delete-form *ngIf="showDelete"
                    [callback]="theDeleteCallback"
                    [itemId]="selected?.ID"
                    [method]="'callback'"
                    [prompt]="'Are you sure you want to delete the question [' + [selected?.Name] + ']?'"                                         
                    (onCancel)="showDelete=false;"
                ></delete-form>  
                <d3s-admin-survey-question-editor *ngIf="showEditor" [questionId]="selected?.ID" [surveyTypeId]="survey?.ID" (saveClick)="saveQuestion($event)" (closeClick)="closeEditor()"></d3s-admin-survey-question-editor>               
                `
})

export class AdminSurveyQuestionsComponent extends BaseComponent implements OnChanges {
    @Input() survey: SurveyType = null;
    error: any;
    questions: SurveyQuestionType[] = [];
    showEditor: boolean = false;
    showDelete: boolean = false;
    
    selected: SurveyQuestionType = null;
    theDeleteCallback: Function;

    constructor(private surveysService: SurveysService) {
        super();
        this.theDeleteCallback = this.deleteQuestion.bind(this);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.survey != null) this.getQuestions();
    }

    getQuestions() {
        this.isLoading = true;
        this.surveysService
            .getSurveyTypeQuestions(this.survey)
            .then(res => {
                this.questions = res;
                this.isLoading = false;
            })
            .catch(error => this.error = error);
    }

    deleteQuestion(id: number) {
        this.surveysService.deleteSurveyQuestionType(id);
        this.showDelete = false;
        this.questions.splice(this.findQuestionById(id), 1);
    }

    add() {
        this.showEditor = true;
        this.selected = null;
    }

    closeEditor() {
        this.showEditor = false;
        if (this.selected == null && this.questions.length > 0)
            this.selected = this.questions[0];
    }

    findQuestionById(id: number) {
        var index: number = -1;
        for (var question of this.questions) {
            index++;
            if (question.ID == id) return index;
        }
    }
    
    saveQuestion(event) {
        this.surveysService.saveSurveyTypeQuestion(event.question)
            .then(result => {
                this.getQuestions(); // incompatible types reload
                this.showEditor = false;
            });
    }
}


