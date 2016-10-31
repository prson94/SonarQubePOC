
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { GridFilterExpression, GridRelationshipFilterExpression, GridFilterFieldType, GridAttributeFilterExpression } from '../models/grid-definition.model';
import { RuleType, RuleDimension, Rule, RuleDetail, RuleResultPagedResults, RuleResultFilter } from '../models/rule.model';
import { JsonResult } from '../models/jsonresult.model';
import { SortOrder } from '../models/enums.model';

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

    getResultsByRule(id: number, pageNumber?: number, pageSize?: number, sortField?: string, sortOrder?: SortOrder, filters?: GridFilterExpression[], relationships?: GridRelationshipFilterExpression, attributes?: GridAttributeFilterExpression, simpleFilter?: string): Promise<RuleResultPagedResults> {
        let sortOrderText = sortOrder == SortOrder.None ? "" : (sortOrder == SortOrder.Descending ? "desc" : "asc");
        let uri = `internal/monitor/rules/${id}/results?pagesize=${pageSize}&pagenum=${pageNumber}&sortDataField=${sortField}&sortOrder=${sortOrderText}`;

        if (filters != undefined) {
            //regular fields
            let normalFilters = filters.filter(f => f.fieldtype == GridFilterFieldType.Normal);
            let count = 0;
            uri += '&filterscount=' + normalFilters.length;

            for (let filter of normalFilters) {
                uri += `&filterdatafield${count}=${filter.field}&filtercondition${count}=${filter.condition}&filtervalue${count}=${filter.value}`;
                count++;
            }

            //related filter fields
            let rellFilters = filters.filter(f => f.fieldtype == GridFilterFieldType.Relation);
            count = 0;

            uri += '&relfilterscount=' + rellFilters.length;

            for (let filter of rellFilters) {
                uri += `&relfilterdatafield${count}=${filter.field.replace("Field", "")}&relfiltercondition${count}=${filter.condition}&relfiltervalue${count}=${filter.value}`;
                count++;
            }

            //hiden filter fields
            let hidFilters = filters.filter(f => f.fieldtype == GridFilterFieldType.Hidden);
            count = 0;

            uri += '&hidfilterscount=' + hidFilters.length;

            for (let filter of hidFilters) {
                uri += `&hidfilterdatafield${count}=${filter.field.replace("Field", "")}&hidfiltercondition${count}=${filter.condition}&hidfiltervalue${count}=${filter.value}`;
                count++;
            }
        }

        if (attributes != undefined) {
            uri += `&AttributeSearchValue=${attributes.attributeSearchValue}&AttributeType=${attributes.attributeType}`;
        }

        if (relationships != undefined) {
            uri += `&RelationshipIncludeType=${relationships.includeType}&RelationshipObjectType=${relationships.relationshipType.TargetType.replace("Type", "")}&RelationshipObjectIDs=${relationships.objectIds.join(",")}`;
        }

        if (simpleFilter != undefined) {
            uri += `&filter=${simpleFilter}`;
        }

        return this.http.get(uri)
            .toPromise()
            .then(response => <RuleResultPagedResults>response.json())
            .catch(err => this.handleError(err));
    }

    getResultsByRuleExcel(id: number) {
        window.location.assign(`internal/monitor/ExportResultsByRule?id=${id}`);
    }

    deleteDimension(id: number): Promise<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'ruledimension', id);
    }
        

    saveDimension(ruleDimension: RuleDimension): Promise<JsonResult> {
        if (ruleDimension.ID == undefined || !ruleDimension.ID) {
            return this.postDynamic(this.http, 'ruledimension', ruleDimension);
        }
        return this.putDynamic(this.http, 'ruledimension', ruleDimension);  
    }

    
}