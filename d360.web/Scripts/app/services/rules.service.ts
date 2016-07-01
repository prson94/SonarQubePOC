///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { RuleType, RuleDimension } from '../models/rule.model';
import { JsonResult } from '../models/jsonresult.model';

@Injectable()
export class RulesService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getRuleTypes(): Promise<RuleType[]> {
        return this.http.get('api/ruletypes')
            .toPromise()
            .then(response => <RuleType[]>response.json().ruleTypes)
            .catch(err => this.handleError(err));
    }

    getRuleDimensions(): Promise<RuleDimension[]> {
        return this.http.get('api/ruledimensions')
            .toPromise()
            .then(response => <RuleDimension[]>response.json())
            .catch(err => this.handleError(err));
    }
}