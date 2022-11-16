import { Component, Input, OnChanges, SimpleChange } from '@angular/core';
import { QuestionTypeV2, SurveyTypeV2 } from '../../../models/survey.model';
import { SurveysService } from '../../../services/surveys.service';
import { BaseComponent } from '../../shared/base.component';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-admin-survey-questions',
    providers: [SurveysService],
    template: `
               <header *ngIf="!showEditor && !showDelete"><ng-container i18n>Questions</ng-container>
                <d3s-tile-actions [hasAdd]="true" (addClick)="add()" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
               </header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <span *ngIf="!isLoading && !showDelete && !showEditor">
                    <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" i18n-placeholder placeholder="Search..." class="grid-simple-filter">
                    <p-table #dt [value]="questions" selectionMode="single" [metaKeySelection]="true" [globalFilterFields]="['Name','DisplayStyle']" sortField="Name" [sortOrder]="1" [pageLinks]="3" [paginator]="true" [rows]="10" [(selection)]="selected">
                        <ng-template pTemplate="header">
                            <tr>
                                <th [pSortableColumn]="'Name'">
                                    <ng-container i18n>Name</ng-container>
                                    <d3s-sortIcon [field]="'Name'"></d3s-sortIcon>
                                </th>
                                <th [pSortableColumn]="'DisplayStyle'">
                                    <ng-container i18n>Display Type</ng-container>
                                    <d3s-sortIcon [field]="'DisplayStyle'"></d3s-sortIcon>
                                </th>
                                <th style="width: 40px"></th>
                                <th style="width: 40px"></th>
                                <th style="width: 40px"></th>
                            </tr>
                            <tr [hidden]="showSimpleFilter">
                                <th><d3s-column-filter [field]="'Name'" [datatype]="'text'"></d3s-column-filter></th>
                                <th><d3s-column-filter [field]="'DisplayStyle'" [datatype]="'text'"></d3s-column-filter></th>
                                <th></th>
                                <th></th>
                                <th></th>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="body" let-item>
                            <tr (dblclick)="selected=item;showEditor=true" [pSelectableRow]="item">
                                <td>{{item.Name}}</td>
                                <td>{{item.DisplayStyle}}</td>
                                <td><d3s-preview-tooltip objectType="QuestionType" [uid]="item.Uid" icon="info"></d3s-preview-tooltip></td>
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
                    [itemId]="selected?.Uid"
                    [method]="'callback'"
                    [prompt]="deletePromptText"                                         
                    (onCancel)="showDelete=false;"
                ></d3s-delete-form>  
                <d3s-admin-survey-question-editor *ngIf="showEditor" [question]="selected" [surveyTypeUid]="survey?.Uid" (saveClick)="saveQuestion($event)" (closeClick)="closeEditor()"></d3s-admin-survey-question-editor>               
                `
})

export class AdminSurveyQuestionsComponent extends BaseComponent implements OnChanges {
    @Input() survey: SurveyTypeV2 = null;
    error: any;

    get questions() {
        return this.survey?.Questions;
    }
    
    showEditor: boolean = false;
    showDelete: boolean = false;

    selected: QuestionTypeV2 = null;
    theDeleteCallback: Function;

    get deletePromptText(): string {
        return $localize`Are you sure you want to delete the question [${this.selected?.Name}]?`;
    }

    constructor(
        private messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        private surveysService: SurveysService) {
        super(settingsService);
        this.theDeleteCallback = this.deleteQuestion.bind(this);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes['survey'] && changes['survey'].previousValue !== changes['survey'].currentValue) {
            this.showEditor = false;
            this.showDelete = false;
        }
    }

    deleteQuestion(uid: string) {
        this.surveysService
            .deleteSurveyQuestionType({ 
                surveyTypeUid: this.survey.Uid, 
                questionTypeUid: uid 
            }).subscribe((result) => {
                if (result == null) {
                    return;
                }

                this.messagesService.showInfoMessage(
                    null,
                    $localize`Success`
                );
                
                this.showDelete = false;
                this.survey.Questions.splice(this.findQuestionById(uid), 1);
            });
    }

    add() {
        this.showEditor = true;
        this.selected = null;
    }

    closeEditor() {
        this.showEditor = false;
        if (this.selected == null && this.questions.length > 0)
            {this.selected = this.questions[0];}
    }

    findQuestionById(uid: string) {
        return this.survey.Questions.findIndex((x) => x.Uid === uid);
    }

    saveQuestion(event) {
        this.surveysService.saveSurveyTypeQuestion(event.surveyTypeUid, event.question)
            .subscribe((result) => {
                if (result == null) {
                    return;
                }

                this.messagesService.showInfoMessage(
                    null,
                    $localize`Success`
                );
                
                this.updateQuestion(event.question, result);
                this.showEditor = false;
            });
    }

    updateQuestion(question: QuestionTypeV2, saveResponse: { Uid: string }) {
        if (question.Uid === null) {
            question.Uid = saveResponse.Uid;
            this.survey.Questions.push(question);
        } else {
            this.survey.Questions[this.findQuestionById(question.Uid)] = question;
        }
    } 
}


