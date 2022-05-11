import { Component, Input, Output, EventEmitter, ChangeDetectorRef, OnChanges, SimpleChanges } from '@angular/core';
import { SurveysService } from '../../../services/surveys.service';
import { BaseComponent } from '../../shared/base.component';
import { Survey, SurveyQuestionType, SurveyQuestionTypeDetails, SurveyQuestionOption, SurveyTypeDisplayStyle, SurveyTypeDetails, Question, SurveyResultsApiModel, SurveyQuestionResponseApiModel } from '../../../models/survey.model';
import { CompanySettingsService } from '../../../services/settings.service';


@Component({
    selector: 'd3s-take-survey',
    providers: [SurveysService],
    templateUrl: 'take-survey.component.html'
})

export class TakeSurveyComponent extends BaseComponent implements OnChanges {

    @Input() surveyType: Survey;
    @Input() assetUid: string;
    @Input() showSurvey: boolean = false;
    @Input() ShowCloseButton: boolean = false;
    @Input() isModalVisible: boolean = false;

    @Output() surveyComplete = new EventEmitter();
    @Output() surveyCancel = new EventEmitter();
    @Output() surveyBack = new EventEmitter();


    private currentQuestionIndex: number = 0;
    private errorMessage: string = '';
    surveyDetails: SurveyTypeDetails;
    SurveyTypeDisplayStyle = SurveyTypeDisplayStyle;

    private submitting: boolean = false;
    private questionDetails: SurveyQuestionTypeDetails[] = [];
    private currentQuestion: Question;


    constructor(
        protected settingsService: CompanySettingsService,
        private surveysService: SurveysService,
        private ref: ChangeDetectorRef) {
        super(settingsService);
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes.surveyType && (changes.surveyType.previousValue !== changes.surveyType.currentValue)) {
            if (changes.surveyType.currentValue) {
                this.questionDetails = [];
                this.load();
            }
        }  
    }

    private load() {
        this.isLoading = true;
        this.submitting = false;
        this.surveyDetails = null;
        this.surveysService.getSurveyTypeDetails(this.surveyType.SurveyTypeUid).subscribe(res => {
            this.surveyDetails = res;
            this.currentQuestion = this.surveyDetails.Questions[0];
            this.isLoading = false;
            this.ref.markForCheck();
        });
    }

    closeDialog() {
        this.currentQuestionIndex = 0;
        this.surveyDetails.Questions.forEach((qd) => { qd.Options.forEach((i) => { i.Value = null; i.IsChecked = false }) });
        this.currentQuestion = this.surveyDetails.Questions[0];
        this.ref.markForCheck();
        this.surveyBack.emit();
    }

    private onSubmit() {
        if (!this.isValid()) return;
        this.submitting = true;
        this.currentQuestion = null;

        this.surveysService.saveSurveyResponse(this.surveyDetails.Uid, this.getSurveyResponseObject()).subscribe(res => {
            this.submitting = false
            this.surveyComplete.emit(res);
        });
    }

    private isValid(): boolean {
        this.errorMessage = '';
        if (this.currentQuestion.Value == undefined) {
            this.errorMessage = $localize`You must select at least one answer`;
        }

        return this.errorMessage.length > 0 ? false : true;
    }

    private nextQuestion(currentIndex: number) {

        if (!this.isValid()) return;

        if (currentIndex < 0 || currentIndex + 1 >= this.surveyDetails.Questions.length) {
            console.log("ERROR - CANNOT MOVE TO NEXT QUESTION INVALID ARRAY ARGUMENTS.");

            return;
        }
        this.currentQuestion = this.surveyDetails.Questions[++this.currentQuestionIndex];
    }

    private previousQuestion(currentIndex: number) {
        if (currentIndex - 1 < 0) {
            console.log("ERROR - CANNOT MOVE TO PREVIOUS QUESTION INVALID ARRAY ARGUMENTS.");

            return;
        }
        this.currentQuestion = this.surveyDetails.Questions[--this.currentQuestionIndex];
    }

    private getSurveyResponseObject(): SurveyResultsApiModel {
        let surveyResponse = new SurveyResultsApiModel();
        surveyResponse.AssetUid = this.assetUid;
        this.surveyDetails.Questions.forEach(x => {
            let q = new SurveyQuestionResponseApiModel();
            q.Comments = x.Comments;
            q.Responses = Array.isArray(x.Value) ? x.Value : [x.Value];
            q.SurveyQuestionUid = x.Uid;
            surveyResponse.Questions.push(q)
        });
        return surveyResponse;
    }
}


