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

    deleteDimension(id: number) {
        let headers = new Headers();
        headers.append('Content-Type', 'application/json');

        let url = `form/dynamicedit/delete/ruledimension/${id}`;

        return this.http
            .delete(url, headers)
            .toPromise()
            .catch(err => this.handleError(err));
    }

    saveDimension(ruleDimension: RuleDimension): Promise<JsonResult> {
        if (ruleDimension.ID == undefined || !ruleDimension.ID) {
            return this.post(ruleDimension);
        }
        return this.put(ruleDimension);  
    }

    private post(ruleDimension: RuleDimension): Promise<JsonResult> {
        let headers = new Headers({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8' //pass as text since its a dynamic object and mvc has issue with dynamic models
        });
        return this.http
            .post("form/dynamicedit/create/ruledimension", 'json=' +JSON.stringify(ruleDimension), { headers: headers })
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(this.handleError);
    }

    private put(ruleDimension: RuleDimension): Promise<JsonResult> {
        let headers = new Headers({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8' //pass as text since its a dynamic object and mvc has issue with dynamic models
        });        
        return this.http
            .put("form/dynamicedit/edit/ruledimension", 'json='+JSON.stringify(ruleDimension), { headers: headers })
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(this.handleError);
    }
}