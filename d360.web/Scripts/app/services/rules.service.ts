import { Injectable } from '@angular/core';
import { Headers, Http, ResponseContentType, Response } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { GridFilterExpression, GridRelationshipFilterExpression, GridFilterFieldType, GridAttributeFilterExpression } from '../models/grid-definition.model';
import { RuleType, Rule, RuleDetail, RuleImplementation, RuleImplementationDetail, RuleResultPagedResults, RuleResultFilter } from '../models/rule.model';
import { JsonResult } from '../models/jsonresult.model';
import { SortOrder } from '../models/enums.model';

@Injectable()
export class RulesService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getRuleTypes(): Promise<RuleType[]> {
        return this.http.get('api/ruletypes')
            .toPromise()
            .then(response => <RuleType[]>response.json())//.ruleTypes)
            .catch(err => this.handleError(err));
    }

    getRules(id: number): Promise<any[]> {
        return this.http.get(`api/rules/${id}`)
            .toPromise()
            .then(response => <any[]>response.json())
            .catch(err => this.handleError(err));
    }

    getRuleImplementations(id: number): Promise<RuleImplementation[]> {
        return this.http.get(`api/rules/${id}/implementations`)
            .toPromise()
            .then(response => <RuleImplementation[]>response.json())
            .catch(err => this.handleError(err));
    }

    getRule(id: number): Promise<RuleDetail> { 
        return this.http.get(`api/rule/${id}`)
            .toPromise()
            .then(response => <RuleDetail>response.json())
            .catch(err => this.handleError(err));
    }

    getRuleImplementation(id: number): Promise<RuleImplementationDetail> {
        return this.http.get(`api/ruleimplementations/${id}`)
            .toPromise()
            .then(response => <RuleImplementationDetail>response.json())
            .catch(err => this.handleError(err));
    }

    getRuleType(id: number): Promise<RuleType> {
        return this.http.get(`api/ruletypes/${id}`)
            .toPromise()
            .then(response => <RuleType>response.json())
            .catch(err => this.handleError(err));
    }

    deleteRule(id: number): Promise<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'rule', id);
    }

    saveRule(rule: Rule): Promise<JsonResult> {
        if (rule.ID == undefined || !rule.ID) {
            return this.postDynamic(this.http, 'rule', rule);
        }
        return this.putDynamic(this.http, 'rule', rule);
    }

    deleteRuleType(id: number): Promise<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'ruletype', id);
    }

    saveRuleType(ruleType: RuleType): Promise<JsonResult> {
        if (ruleType.ID == undefined || !ruleType.ID) {
            return this.postDynamic(this.http, 'ruletype', ruleType);
        }
        return this.putDynamic(this.http, 'ruletype', ruleType);
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
        this.http.get(`internal/monitor/ExportResultsByRule?id=${id}`, { responseType: ResponseContentType.Blob }).subscribe(data => this.downloadFile(data));
    }

    downloadFile(data: Response) {
        var filename = `Rule Data ${new Date().toDateString()}.xlsx`;
        if (window.navigator.msSaveOrOpenBlob) {
            window.navigator.msSaveOrOpenBlob(data.blob(), filename);
        }
        else {
            var url = window.URL.createObjectURL(data.blob());
            var anchor = document.createElement("a");
            anchor.setAttribute("style", "display:none;");
            document.body.appendChild(anchor);
            anchor.setAttribute("download", filename);
            anchor.href = url;
            anchor.click();
        }
    }


    deleteRuleImplementation(id: number): Promise<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'ruleimplementation', id);
    }


    saveRuleImplementation(implementation: RuleImplementation, action: string): Promise<JsonResult> {
        if (action && action == "Copy") {
            return this.postDynamic(this.http, 'ruleimplementation', implementation, undefined,true);
        }
        else if (implementation.ID == undefined || !implementation.ID) {
            return this.postDynamic(this.http, 'ruleimplementation', implementation);
        } else 
        return this.putDynamic(this.http, 'ruleimplementation', implementation);
    }
    
}