
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { SurveyType, SurveyQuestionType, SurveyQuestionTypeDetails, SurveyResponse } from '../models/survey.model';
import { JsonResult } from '../models/jsonresult.model';

@Injectable()
export class SurveysService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getSurveyTypes(): Promise<SurveyType[]> {
        return this.http.get(`api/surveys`)
            .toPromise()
            .then(response => <SurveyType[]>response.json())
            .catch(err => this.handleError(err));
    }

    getSurveyTypeQuestions(survey: SurveyType): Promise<SurveyQuestionType[]> {
        return this.http.get(`api/surveys/${survey.ID}/questions`)
            .toPromise()
            .then(response => <SurveyQuestionType[]>response.json())
            .catch(err => this.handleError(err));
    }

    getSurveyTypeQuestionDetails(id: number, surveyTypeId: number): Promise<SurveyQuestionTypeDetails> {
        return this.http.get(`form/questiontype_formdata?id=${id}&surveyTypeID=${surveyTypeId}`)
            .toPromise()
            .then(response => <SurveyQuestionTypeDetails>response.json())
            .catch(err => this.handleError(err));
    }

    deleteSurveyTypeById(id: number) {
        return this.deleteDynamic(this.http, 'surveytype', id);
    }


    deleteSurveyQuestionType(id: number) {
        return this.deleteDynamic(this.http, 'surveyquestiontype', id);
    }


    saveSurveyType(surveyType: SurveyType): Promise<JsonResult> {
        if (surveyType.ID == undefined || !surveyType.ID) {
            return this.postDynamic(this.http, 'surveytype', surveyType);
        }
        return this.putDynamic(this.http, 'surveytype', surveyType);
    }

    saveSurveyTypeQuestion(surveyQuestion: SurveyQuestionTypeDetails): Promise<JsonResult>{
        if (surveyQuestion.ID == undefined || !surveyQuestion.ID) {         
            return this.addSurveyTypeQuestion(surveyQuestion);
        }        
        return this.editSurveyTypeQuestion(surveyQuestion);
    }

    protected addSurveyTypeQuestion(surveyQuestion: SurveyQuestionTypeDetails): Promise<JsonResult> {        
        let headers = new Headers({
            'Content-Type': 'application/json'
        });

        return this.http
            .post('form/AddQuestionType', JSON.stringify(surveyQuestion), { headers: headers })
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(this.handleError);
    }

    protected editSurveyTypeQuestion(surveyQuestion: SurveyQuestionTypeDetails): Promise<JsonResult> {        
        let headers = new Headers({
            'Content-Type': 'application/json'
        });

        return this.http
            .put('form/EditQuestionType/', JSON.stringify(surveyQuestion), { headers: headers })
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(this.handleError);
    }

    getObjectSurvey(parentObjectID: number, parentObjectType: string, objectID: number, objectType: string): Promise<SurveyType> {
        return this.http.get(`api/surveys/${parentObjectType}/${parentObjectID}/${objectType}/${objectID}/survey`)
            .toPromise()
            .then(response => <SurveyType>response.json())
            .catch(err => this.handleError(err));
    }

    saveSurveyResponse(response: SurveyQuestionTypeDetails[], surveyId: number, objectType:string, objectId: number): Promise<JsonResult> {
        let headers = new Headers({
            'Content-Type': 'application/json'
        });

        let surveyResponse = new SurveyResponse();
        for (let question of response) {
            question.Values = question.Items;
        }
        surveyResponse.Questions = response;

        return this.http
            .post(`api/survey/${surveyId}/${objectId}/${objectType}`, JSON.stringify(surveyResponse), { headers: headers })
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(this.handleError);
    }

}