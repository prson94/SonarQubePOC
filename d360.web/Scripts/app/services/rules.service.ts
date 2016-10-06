
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { RuleType, RuleDimension, Rule, RuleDetail } from '../models/rule.model';
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

    getRules(): Promise<Rule[]> {
        return this.http.get('api/rules')
            .toPromise()
            .then(response => <Rule[]>response.json())
            .catch(err => this.handleError(err));
    }

    getRule(id: number): Promise<RuleDetail> {
        return this.http.get(`api/rule/${id}`)
            .toPromise()
            .then(response => <RuleDetail>response.json())
            .catch(err => this.handleError(err));
    }

    deleteRule(id: number) {
        return this.deleteDynamic(this.http, 'rule', id);
    }

    saveRule(rule: Rule): Promise<JsonResult> {
        if (rule.ID == undefined || !rule.ID) {
            return this.postDynamic(this.http, 'rule', rule);
        }
        return this.putDynamic(this.http, 'rule', rule);
    }

    getRuleDimensions(): Promise<RuleDimension[]> {
        return this.http.get('api/ruledimensions')
            .toPromise()
            .then(response => <RuleDimension[]>response.json())
            .catch(err => this.handleError(err));
    }

    deleteDimension(id: number) {
        return this.deleteDynamic(this.http, 'ruledimension', id);
    }
        

    saveDimension(ruleDimension: RuleDimension): Promise<JsonResult> {
        if (ruleDimension.ID == undefined || !ruleDimension.ID) {
            return this.postDynamic(this.http, 'ruledimension', ruleDimension);
        }
        return this.putDynamic(this.http, 'ruledimension', ruleDimension);  
    }

    
}