import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { NgForm } from '@angular/forms';
import { SurveysService  } from '../../services/surveys.service';
import { BaseComponent } from '../shared/base.component';
import { SurveyType, SurveyQuestionType, SurveyQuestionTypeDetails, SurveyQuestionOption, SurveyTypeDisplayStyle } from '../../models/survey.model';
import { Router } from '@angular/router';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

@Component({
    selector: 'd3s-take-survey',
    providers: [SurveysService],    
    template: `
                <header>Survey - {{surveyType.Name}}</header>
               <form (ngSubmit)="onSubmit()" #surveyForm="ngForm">
                    <div style="padding:20px">
                    <div class="row" *ngIf="currentQuestion">
                        <h4 style="padding-bottom:10px"><span *ngIf="questions.length > 1">{{currentQuestionIndex+1}} - </span>{{currentQuestion.Name}}</h4>
                        <span *ngIf="currentQuestion.Description" [innerHtml]="currentQuestion.Description"></span>
                        <span [ngSwitch]="currentQuestion.DisplayStyle">
                            <span *ngSwitchCase="SurveyTypeDisplayStyle.RadioList">
                                <div *ngFor="let option of currentQuestion?.Items" style="padding:2px"><label><input type="radio" name="options" (click)="option.IsChecked=$event.target.checked" [value]="option.Value">{{option.Name}}</label></div>
                            </span>
                            <span *ngSwitchCase="SurveyTypeDisplayStyle.CheckList">
                                <div *ngFor="let option of currentQuestion?.Items" style="padding:2px"><label><input type="checkbox" name="options" [(ngModel)]="option.IsChecked" [value]="option.Value">{{option.Name}}</label></div>
                            </span>
                        </span>
                        <div class="col s12">
                            <div class="FieldName">Comments</div>
                            <textarea name="comments" [style]="{'height':'150px'}" [(ngModel)]="currentQuestion.Comments"></textarea>
                        </div>                    
                        <div class="col s12">&nbsp;</div>
                        <div class="col s12">
                            <button *ngIf="currentQuestionIndex > 0" pButton type="button" [disabled]="!surveyForm.form.valid" label="Previous" (click)="previousQuestion(currentQuestionIndex)"></button>
                            <button *ngIf="currentQuestionIndex + 1 < questions.length" pButton type="button" [disabled]="!surveyForm.form.valid" label="Next" (click)="nextQuestion(currentQuestionIndex)"></button>                            
                            <button *ngIf="currentQuestionIndex+1 == questions.length" pButton type="submit" [disabled]="!surveyForm.form.valid" label="Save"></button> 
                            <button *ngIf="ShowCloseButton" pButton type="button" label="Close" (click)="surveyBack.emit()"></button>  
                            <em *ngIf="questions.length > 1">Question {{currentQuestionIndex+1}} of {{questions.length}}</em>
                        </div>      
                    </div>              
                    </div>
               </form>               
                `
})

export class TakeSurveyComponent extends BaseComponent implements OnInit {
    @Input() surveyType: SurveyType;
    
    @Output() surveyComplete = new EventEmitter();
    @Output() surveyCancel = new EventEmitter();
    @Output() surveyBack = new EventEmitter();

    @Input() objectType: string;
    @Input() objectID: number;
    @Input() ShowCloseButton: boolean = false;
    
    private questions: SurveyQuestionType[] = [];    
    private currentQuestionIndex: number = 0;

    SurveyTypeDisplayStyle= SurveyTypeDisplayStyle;

    private questionDetails: SurveyQuestionTypeDetails[] = [];
    private currentQuestion: SurveyQuestionTypeDetails;
    

    constructor(private surveysService: SurveysService) {
        super();        
    }    

    ngOnInit() {
        this.load();
    }

    private load() {
        this.isLoading = true;
        this.surveysService.getSurveyTypeQuestions(this.surveyType)
            .subscribe(result => {
                this.questions = result;
                if (this.questions.length > 0) {
                    this.loadQuestionDetails(this.questions[0]);
                }
                this.isLoading = false;
            });
    }

    private loadQuestionDetails(question: SurveyQuestionType) {
        var array = this.questionDetails.filter(x => x.ID == question.ID);
        if (array.length > 0) {
            this.currentQuestion = array[0];
        }
        else {
            this.isLoading = true;
            this.surveysService.getSurveyTypeQuestionDetails(question.ID, this.surveyType.ID)
                .subscribe(result => {
                    this.currentQuestion = result;
                    for (let option of this.currentQuestion.Items) {
                        option.IsChecked = false;
                    }
                    this.questionDetails.push(result);
                    this.isLoading = false;
                });
        }
    }

    private onSubmit() {
        this.surveysService.saveSurveyResponse(this.questionDetails, this.surveyType.ID, this.objectType, this.objectID).subscribe(res => {
            this.surveyComplete.emit(res);
        });
    }

    private nextQuestion(currentIndex: number) {
        
        if (currentIndex < 0 || currentIndex + 1 >= this.questions.length) {
            console.log("ERROR - CANNOT MOVE TO NEXT QUESTION INVALID ARRAY ARGUMENTS.");

            return;
        }
                
        this.loadQuestionDetails(this.questions[++this.currentQuestionIndex]);
    }

    private previousQuestion(currentIndex: number) {
        if (currentIndex- 1 < 0) {
            console.log("ERROR - CANNOT MOVE TO PREVIOUS QUESTION INVALID ARRAY ARGUMENTS.");

            return;
        }

        this.loadQuestionDetails(this.questions[--this.currentQuestionIndex]);
    }

    private selectRadioValue(event, option) {
        //console.log(event);
    }
}


