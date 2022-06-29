import { Injectable } from '@angular/core';
import { SurveyType, SurveyQuestionType, SurveyTypeDetails, SurveyQuestionTypeDetails, SurveyResultsApiModel, Survey } from '../models/survey.model';
import { JsonResult } from '../models/jsonresult.model';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';
import { ApiErrorOrResponse } from '../models/ApiErrorOrResponse';

@Injectable({
    providedIn: 'root'
})
export class SurveysService extends BaseObservableService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    getSurveyTypes(): Observable<SurveyType[]> {
        return this.http.get(`api/surveys`)
            .pipe(
                map(response => <SurveyType[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getSurveyTypeDetails(surveyTypeUid: string): Observable<SurveyTypeDetails> {
        return this.http.get(`api/v2/survey/types?SurveyTypeUid=${surveyTypeUid}`)
            .pipe(
                map((response: any) => { return response.items[0]; }),
                catchError(err => this.handleError(err))
            );
    }

    //used by admin section need to convert to v2 API 
    getSurveyTypeQuestionDetails(id: number, surveyTypeId: number): Observable<SurveyQuestionTypeDetails> {
        return this.http.get(`form/questiontype_formdata?id=${id}&surveyTypeID=${surveyTypeId}`)
            .pipe(
                map(response => <SurveyQuestionTypeDetails>response),
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

    deleteSurveyTypeById(uid: string): Observable<boolean> {
        return this.http
            .delete(`/api/v2/survey/types/${uid}`)
            .pipe(
                map(res => true),
                catchError(err => this.handleError(err))
            );
    }


    deleteSurveyQuestionType(id: number): Observable<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'surveyquestiontype', id);
    }


    saveSurveyType(surveyType: SurveyType): Observable<SurveyType> {
        const headers = new HttpHeaders({
            'Content-Type': 'application/json'
        });

        if (surveyType.Uid == null) {
            return this.http.post(
                    '/api/v2/survey/types', 
                    JSON.stringify(surveyType),
                     {headers }
                )
                .pipe(
                    map(res => res as SurveyType),
                    catchError(err => this.handleError(err))
                );
        }
        
        return this.http.put(
            `/api/v2/survey/types/${surveyType.Uid}`, 
            JSON.stringify(surveyType),
            { headers }
        )
        .pipe(
            map(res => res as SurveyType),
            catchError(err => this.handleError(err))
        );
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

    getObjectSurvey(assetUid: string): Observable<Survey> {
        return this.http.get(`api/v2/survey/${assetUid}`)
            .pipe(
                map(response => <Survey>response),
                catchError(err => this.handleError(err))
            );
    }

    saveSurveyResponse(surveyUid: string, response: SurveyResultsApiModel): Observable<JsonResult> {
        let headers = new HttpHeaders({
            'Content-Type': 'application/json'
        });

        return this.http
            .post(`api/v2/survey/${surveyUid}`, JSON.stringify(response), { headers })
            .pipe(
                map(res => <JsonResult>res),
                catchError(err => this.handleError(err))
            );
    }

}