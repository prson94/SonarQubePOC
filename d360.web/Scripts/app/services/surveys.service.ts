import { Injectable } from '@angular/core';
import { SurveyType, SurveyQuestionType, SurveyQuestionTypeDetails, SurveyResponse } from '../models/survey.model';
import { JsonResult } from '../models/jsonresult.model';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';

@Injectable()
export class SurveysService extends BaseObservableService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    getSurveyTypes(): Observable<SurveyType[]> {
        return this.http.get(`api/surveys`)
            .pipe(
                map(response => <SurveyType[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getSurveyTypeQuestions(survey: SurveyType): Observable<SurveyQuestionType[]> {
        return this.http.get(`api/surveys/${survey.ID}/questions`)
            .pipe(
                map(response => <SurveyQuestionType[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getSurveyTypeQuestionDetails(id: number, surveyTypeId: number): Observable<SurveyQuestionTypeDetails> {
        return this.http.get(`form/questiontype_formdata?id=${id}&surveyTypeID=${surveyTypeId}`)
            .pipe(
                map(response => <SurveyQuestionTypeDetails>response),
                catchError(err => this.handleError(err))
            );
    }

    deleteSurveyTypeById(id: number): Observable<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'surveytype', id);
    }


    deleteSurveyQuestionType(id: number): Observable<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'surveyquestiontype', id);
    }


    saveSurveyType(surveyType: SurveyType): Observable<JsonResult> {
        if (surveyType.ID == undefined || !surveyType.ID) {
            return this.postDynamic(this.http, 'surveytype', surveyType);
        }
        return this.putDynamic(this.http, 'surveytype', surveyType);
    }

    saveSurveyTypeQuestion(surveyQuestion: SurveyQuestionTypeDetails): Observable<JsonResult> {
        if (surveyQuestion.ID == undefined || !surveyQuestion.ID) {
            return this.addSurveyTypeQuestion(surveyQuestion);
        }
        return this.editSurveyTypeQuestion(surveyQuestion);
    }

    protected addSurveyTypeQuestion(surveyQuestion: SurveyQuestionTypeDetails): Observable<JsonResult> {
        let headers = new HttpHeaders({
            'Content-Type': 'application/json'
        });

        return this.http
            .post('form/AddQuestionType', JSON.stringify(surveyQuestion), { headers })
            .pipe(
                map(res => <JsonResult>res),
                catchError(err => this.handleError(err))
            );
    }

    protected editSurveyTypeQuestion(surveyQuestion: SurveyQuestionTypeDetails): Observable<JsonResult> {
        let headers = new HttpHeaders({
            'Content-Type': 'application/json'
        });

        return this.http
            .put('form/EditQuestionType/', JSON.stringify(surveyQuestion), { headers })
            .pipe(
                map(res => <JsonResult>res),
                catchError(err => this.handleError(err))
            );
    }

    getObjectSurvey(parentObjectID: number, parentObjectType: string, objectID: number, objectType: string): Observable<SurveyType> {
        return this.http.get(`api/surveys/${parentObjectType}/${parentObjectID}/${objectType}/${objectID}/survey`)
            .pipe(
                map(response => <SurveyType>response),
                catchError(err => this.handleError(err))
            );
    }

    saveSurveyResponse(response: SurveyQuestionTypeDetails[], surveyId: number, objectType: string, objectId: number): Observable<JsonResult> {
        let headers = new HttpHeaders({
            'Content-Type': 'application/json'
        });

        let surveyResponse = new SurveyResponse();
        for (let question of response) {
            question.Values = question.Items;
        }
        surveyResponse.Questions = response;

        return this.http
            .post(`api/survey/${surveyId}/${objectId}/${objectType}`, JSON.stringify(surveyResponse), { headers })
            .pipe(
                map(res => <JsonResult>res),
                catchError(err => this.handleError(err))
            );
    }

}