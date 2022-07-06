import { Injectable } from '@angular/core';
import { SurveyType, SurveyTypeDetails, SurveyQuestionTypeDetails, SurveyResultsApiModel, Survey, SurveyTypesResponse, SurveyTypeV2, QuestionTypeV2 } from '../models/survey.model';
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

    getSurveyTypes({ pageNum, pageSize }: { 
        pageNum: number; 
        pageSize: number;
    }): Observable<SurveyTypesResponse> {
        return this.http.get(`api/v2/survey/types?_pageNum=${pageNum}&_pageSize=${pageSize}`)
            .pipe(
                map(response => response as SurveyTypesResponse),
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

    deleteSurveyTypeById(uid: string): Observable<boolean> {
        return this.http
            .delete(`/api/v2/survey/types/${uid}`)
            .pipe(
                map(res => true),
                catchError(err => this.handleError(err))
            );
    }


    deleteSurveyQuestionType({ surveyTypeUid, questionTypeUid }: { surveyTypeUid: string; questionTypeUid: string; }): Observable<JsonResult> {
        return this.http
            .delete(`/api/v2/survey/types/${surveyTypeUid}/questions/${questionTypeUid}`)
            .pipe(
                map(res => true),
                catchError(err => this.handleError(err))
            );
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
            map(res => surveyType),
            catchError(err => this.handleError(err))
        );
    }

    saveSurveyTypeQuestion(surveyTypeUid: string, surveyQuestion: QuestionTypeV2): Observable<{ Uid: string }> {
        const headers = new HttpHeaders({
            'Content-Type': 'application/json'
        });

        if (surveyQuestion.Uid == null) {
            return this.http.post(
                    `/api/v2/survey/types/${surveyTypeUid}/questions`, 
                    JSON.stringify(surveyQuestion),
                    { headers }
                )
                .pipe(
                    map(res => res as { Uid: string }),
                    catchError(err => this.handleError(err))
                );
        }
        
        return this.http.put(
            `/api/v2/survey/types/${surveyTypeUid}/questions/${surveyQuestion.Uid}`, 
            JSON.stringify(surveyQuestion),
            { headers }
        )
        .pipe(
            map(res => surveyQuestion as { Uid: string }),
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