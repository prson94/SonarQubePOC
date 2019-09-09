import { Injectable } from '@angular/core';
import { GridFilterExpression, GridRelationshipFilterExpression, GridFilterFieldType, GridAttributeFilterExpression } from '../models/grid-definition.model';
import { RuleType, Rule, RuleDetail, RuleImplementation, RuleImplementationDetail, RuleResultPagedResults } from '../models/rule.model';
import { JsonResult } from '../models/jsonresult.model';
import { SortOrder } from '../models/enums.model';
import { HttpClient } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';

@Injectable()
export class RulesService extends BaseObservableService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    getRuleTypes(): Observable<RuleType[]> {
        return this.http.get('api/ruletypes')
            .pipe(
                map(response => <RuleType[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getRules(id: number): Observable<any[]> {
        return this.http.get(`api/rules/${id}`)
            .pipe(
                map(response => <any[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getRuleImplementations(id: number): Observable<RuleImplementation[]> {
        return this.http.get(`api/rules/${id}/implementations`)
            .pipe(
                map(response => <RuleImplementation[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getRule(id: number): Observable<RuleDetail> {
        return this.http.get(`api/rule/${id}`)
            .pipe(
                map(response => <RuleDetail>response),
                catchError(err => this.handleError(err))
            );
    }

    getRuleImplementation(id: number): Observable<RuleImplementationDetail> {
        return this.http.get(`api/ruleimplementations/${id}`)
            .pipe(
                map(response => <RuleImplementationDetail>response),
                catchError(err => this.handleError(err))
            );
    }

    getRuleType(id: number): Observable<RuleType> {
        return this.http.get(`api/ruletypes/${id}`)
            .pipe(
                map(response => <RuleType>response),
                catchError(err => this.handleError(err))
            );
    }
    
    saveRule(rule: Rule): Observable<JsonResult> {
        if (rule.ID == undefined || !rule.ID) {
            return this.postDynamic(this.http, 'rule', rule);
        }
        return this.putDynamic(this.http, 'rule', rule);
    }

    deleteRuleType(id: number): Observable<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'ruletype', id);
    }

    saveRuleType(ruleType: RuleType): Observable<JsonResult> {
        if (ruleType.ID == undefined || !ruleType.ID) {
            return this.postDynamic(this.http, 'ruletype', ruleType);
        }
        return this.putDynamic(this.http, 'ruletype', ruleType);
    }
    
    getResultsByRule(id: number, pageNumber?: number, pageSize?: number, sortField?: string, sortOrder?: SortOrder, filters?: GridFilterExpression[], relationships?: GridRelationshipFilterExpression, attributes?: GridAttributeFilterExpression, simpleFilter?: string): Observable<RuleResultPagedResults> {
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
            .pipe(
                map(response => <RuleResultPagedResults>response),
                catchError(err => this.handleError(err))
            );
    }

    getResultsByRuleExcel(id: number) {
        this.http.get(`internal/monitor/ExportResultsByRule?id=${id}`, { responseType: "blob" }).pipe(
            map((response) => {
                this.downloadFile(response);

            }),
            catchError(err => this.handleError(err))
        ).subscribe();
    }

    downloadFile(data: any) {
        var filename = `Rule Data ${new Date().toDateString()}.xlsx`;
        if (window.navigator.msSaveOrOpenBlob) {
            window.navigator.msSaveOrOpenBlob(data, filename);
        }
        else {
            var url = window.URL.createObjectURL(data);
            var anchor = document.createElement("a");
            anchor.setAttribute("style", "display:none;");
            document.body.appendChild(anchor);
            anchor.setAttribute("download", filename);
            anchor.href = url;
            anchor.click();
        }
    }


    deleteRuleImplementation(id: number): Observable<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'ruleimplementation', id);
    }


    saveRuleImplementation(implementation: RuleImplementation, action: string): Observable<JsonResult> {
        if (action && action == "Copy") {
            return this.postDynamic(this.http, 'ruleimplementation', implementation, undefined, true);
        }
        else if (implementation.ID == undefined || !implementation.ID) {
            return this.postDynamic(this.http, 'ruleimplementation', implementation);
        } else
            return this.putDynamic(this.http, 'ruleimplementation', implementation);
    }
}