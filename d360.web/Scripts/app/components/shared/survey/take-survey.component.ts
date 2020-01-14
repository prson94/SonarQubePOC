import { Component, Input, Output, EventEmitter, OnInit, AfterViewInit, ChangeDetectorRef, OnChanges, SimpleChanges } from '@angular/core';
import { NgForm } from '@angular/forms';
import { SurveysService } from '../../../services/surveys.service';
import { BaseComponent } from '../../shared/base.component';
import { SurveyType, SurveyQuestionType, SurveyQuestionTypeDetails, SurveyQuestionOption, SurveyTypeDisplayStyle } from '../../../models/survey.model';
import { Router } from '@angular/router';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';

@Component({
    selector: 'd3s-take-survey',
    providers: [SurveysService],
    templateUrl: 'take-survey.component.html'
})

export class TakeSurveyComponent extends BaseComponent implements OnChanges {

    @Input() surveyType: SurveyType;

    @Output() surveyComplete = new EventEmitter();
    @Output() surveyCancel = new EventEmitter();
    @Output() surveyBack = new EventEmitter();
    @Input() showSurvey: boolean = false;
    @Input() objectType: string;
    @Input() objectID: number;
    @Input() ShowCloseButton: boolean = false;

    @Input() isModalVisible: boolean = false;
    private questions: SurveyQuestionType[] = [];
    private currentQuestionIndex: number = 0;
    private errorMessage: string = '';
    SurveyTypeDisplayStyle = SurveyTypeDisplayStyle;

    private submitting: boolean = false;
    private questionDetails: SurveyQuestionTypeDetails[] = [];
    private currentQuestion: SurveyQuestionTypeDetails;


    constructor(private surveysService: SurveysService,
        private ref: ChangeDetectorRef) {
        super();
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes.surveyType && (changes.surveyType.previousValue !== changes.surveyType.currentValue)) {
            if (changes.surveyType.currentValue) {
                this.questionDetails = [];
                this.questions = [];
                this.load();
            }
        }  
    }

    private load() {
        this.isLoading = true;
        this.submitting = false;
        this.surveysService.getSurveyTypeQuestions(this.surveyType)
            .subscribe(result => {
                this.questions = result;
                this.questionDetails = [];
                if (this.questions.length > 0) {
                    this.loadQuestionDetails(this.questions[0]);
                }
                this.isLoading = false;
            });
    }

    private loadQuestionDetails(question: SurveyQuestionType) {
        let questions = [ ...this.questionDetails ];
        var localQuestionDetails = questions.filter(x => x.ID == question.ID);
        if (localQuestionDetails.length > 0) {
            this.currentQuestion = localQuestionDetails[0];
            this.ref.markForCheck();
        }
        else {
            this.isLoading = true;
            this.surveysService.getSurveyTypeQuestionDetails(question.ID, this.surveyType.ID)
                .subscribe(result => {
                    this.currentQuestion = result;
                    for (let option of this.currentQuestion.Items) {
                        option.IsChecked = false;
                    }
                    if (questions.indexOf(result) === -1)
                        questions.push(result);
                    this.updateQuestions(questions);
                    this.isLoading = false;
                    this.ref.markForCheck();
                });
        }
    }
    updateQuestions(q) {
        this.questionDetails = q;
    }

    private closeDialog() {
        this.currentQuestionIndex = 0;
        this.questionDetails.forEach(qd => { qd.Items.forEach(i => { i.Value = null; i.IsChecked = false }) });
        this.currentQuestion = this.questionDetails[0];
        this.ref.markForCheck();
        this.surveyBack.emit();
    }

    private onSubmit() {
        if (!this.isValid()) return;
        this.submitting = true;
        this.currentQuestion = null;
        this.surveysService.saveSurveyResponse(this.questionDetails, this.surveyType.ID, this.objectType, this.objectID).subscribe(res => {
            this.questionDetails = [];
            this.questions = [];
            this.submitting = false
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
}


