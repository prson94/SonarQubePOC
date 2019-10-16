import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { NgForm } from '@angular/forms';
import { SurveysService } from '../../services/surveys.service';
import { BaseComponent } from '../shared/base.component';
import { SurveyType, SurveyQuestionType, SurveyQuestionTypeDetails, SurveyQuestionOption, SurveyTypeDisplayStyle } from '../../models/survey.model';
import { Router } from '@angular/router';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

@Component({
    selector: 'd3s-take-survey',
    providers: [SurveysService],
    templateUrl: 'take-survey.component.html'
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
    private errorMessage: string = '';

    SurveyTypeDisplayStyle = SurveyTypeDisplayStyle;

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
        if (!this.isValid()) return;

        this.surveysService.saveSurveyResponse(this.questionDetails, this.surveyType.ID, this.objectType, this.objectID).subscribe(res => {
            this.surveyComplete.emit(res);
        });
    }

    private isValid(): boolean {
        this.errorMessage = '';
        var item = this.currentQuestion.Items.find(x => x.IsChecked == true);
        if (!item) {
            this.errorMessage = 'You must select at least one answer';
        }

        return this.errorMessage.length > 0 ? false : true;
    }

    private nextQuestion(currentIndex: number) {

        if (!this.isValid()) return;

        if (currentIndex < 0 || currentIndex + 1 >= this.questions.length) {
            console.log("ERROR - CANNOT MOVE TO NEXT QUESTION INVALID ARRAY ARGUMENTS.");

            return;
        }

        this.loadQuestionDetails(this.questions[++this.currentQuestionIndex]);
    }

    private previousQuestion(currentIndex: number) {
        if (currentIndex - 1 < 0) {
            console.log("ERROR - CANNOT MOVE TO PREVIOUS QUESTION INVALID ARRAY ARGUMENTS.");

            return;
        }

        this.loadQuestionDetails(this.questions[--this.currentQuestionIndex]);
    }

    private selectRadioValue(event, option) {
        //console.log(event);
    }
}


