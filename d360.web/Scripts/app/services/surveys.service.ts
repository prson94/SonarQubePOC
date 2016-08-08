///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { SurveyType } from '../models/survey.model';

@Injectable()
export class SurveysService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getSurveyTypes(): Promise<SurveyType[]> {
        return this.http.get(`api/surveys`)
            .toPromise()
            .then(response => <SurveyType[]>response.json())
            .catch(err => this.handleError(err));
    }
}