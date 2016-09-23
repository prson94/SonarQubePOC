///<reference path="../../es6-shim.d.ts"/>
import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { NgForm } from '@angular/forms';
import { Predicate } from '../../models/predicate.model';
import { MessagesService, SurveysService  } from '../../services/index';
import { BaseComponent } from '../shared/base.component';
import { SurveyType, SurveyQuestionType, SurveyQuestionTypeDetails, SurveyResponse } from '../../models/survey.model';

@Component({
    selector: 'd3s-take-survey',
    providers: [SurveysService],
    template: `
                <header>Survey - {{surveyType.Name}}</header>
               <form (ngSubmit)="onSubmit()" #surveyForm="ngForm">
                    <div class="row" *ngIf="currentQuestion">
                        <h4><span *ngIf="questions.length > 1">{{currentQuestionIndex+1}} - </span>{{currentQuestion.Name}}</h4>
                        <span [innerHtml]="currentQuestion.Description"></span>
                        <div class="col s12">
                            <div class="FieldName">Comments</div>
                            <textarea name="comments" [style]="{'height':'150px'}" [(ngModel)]="surveyResponse.Comments"></textarea>
                        </div>                    
                        <div class="col s12">&nbsp;</div>
                        <div class="col s12">
                            <button pButton type="button" [disabled]="!surveyForm.form.valid" style="width: '150px';" label="Next"></button>
                            <button *ngIf="currentQuestionIndex == questions.length" pButton type="submit" [disabled]="!surveyForm.form.valid" style="width: '150px';" label="Save"></button>                            
                            <button pButton type="button" (click)="surveyCancel.emit();" label="Close" style="width: '150px';"></button>
                        </div>      
                    </div>              
               </form>               
                `
})

export class TakeSurveyComponent extends BaseComponent implements OnInit {
    @Input() surveyType: SurveyType;

    @Output() surveyComplete = new EventEmitter();
    @Output() surveyCancel = new EventEmitter();

    private surveyResponse: SurveyResponse = new SurveyResponse();
    private questions: SurveyQuestionType[] = [];
    private currentQuestion: SurveyQuestionType;
    private currentQuestionIndex: number = 0;

    constructor(private surveysService: SurveysService) {
        super();        
    }    

    ngOnInit() {
        this.load();
    }

    private load() {
        this.isLoading = true;
        this.surveysService.getSurveyTypeQuestions(this.surveyType)
            .then(result => {
                this.questions = result;
                if (this.questions.length > 0) {
                    this.currentQuestion = this.questions[0];
                }
                this.isLoading = false;
            });
    }

    private onSubmit() {
        this.surveyComplete.emit();
    }
}


