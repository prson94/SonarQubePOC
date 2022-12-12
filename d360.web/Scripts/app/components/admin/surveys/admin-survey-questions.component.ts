import { Component, Input, OnChanges, SimpleChange } from '@angular/core';
import { QuestionTypeV2, SurveyTypeV2 } from '../../../models/survey.model';
import { SurveysService } from '../../../services/surveys.service';
import { BaseComponent } from '../../shared/base.component';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-admin-survey-questions',
    providers: [SurveysService],
    templateUrl: 'admin-survey-questions.component.html'
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


