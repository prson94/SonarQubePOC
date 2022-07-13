import { Injectable } from '@angular/core';
import { SurveyTypeUpsertModel, SurveyTypeDetails, SurveyResultsApiModel, Survey, SurveyTypesResponse, QuestionTypeV2 } from '../models/survey.model';
import { JsonResult } from '../models/jsonresult.model';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';
import { V2ApiFilters } from '../models/asset-search.model';

@Injectable({
    providedIn: 'root'
})
export class SurveysService extends BaseObservableService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    getSurveyTypes(params: V2ApiFilters): Observable<SurveyTypesResponse> {
        const queryString = '?' + Object.keys(params).map((key) => key + '=' + params[key]).join('&');

        return this.http.get(`api/v2/survey/types${queryString}`)
            .pipe(
                map((response) => response as SurveyTypesResponse),
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

    deleteSurveyTypeById(uid: string): Observable<boolean> {
        return this.http
            .delete(`/api/v2/survey/types/${uid}`)
            .pipe(
                map(() => true),
                catchError(err => this.handleError(err))
            );
    }


    deleteSurveyQuestionType({ surveyTypeUid, questionTypeUid }: { surveyTypeUid: string; questionTypeUid: string; }): Observable<JsonResult> {
        return this.http
            .delete(`/api/v2/survey/types/${surveyTypeUid}/questions/${questionTypeUid}`)
            .pipe(
                map(() => true),
                catchError(err => this.handleError(err))
            );
    }


    saveSurveyType(surveyType: SurveyTypeUpsertModel): Observable<SurveyTypeUpsertModel> {
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
                    map((res) => res as SurveyTypeUpsertModel),
                    catchError(err => this.handleError(err))
                );
        }
        
        return this.http.put(
            `/api/v2/survey/types/${surveyType.Uid}`, 
            JSON.stringify(surveyType),
            { headers }
        )
        .pipe(
            map(() => surveyType),
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
                    map((res) => res as { Uid: string }),
                    catchError(err => this.handleError(err))
                );
        }
        
        return this.http.put(
            `/api/v2/survey/types/${surveyTypeUid}/questions/${surveyQuestion.Uid}`, 
            JSON.stringify(surveyQuestion),
            { headers }
        )
        .pipe(
            map(() => surveyQuestion as { Uid: string }),
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