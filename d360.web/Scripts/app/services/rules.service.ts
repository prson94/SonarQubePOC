import { Injectable } from '@angular/core';
import { RuleType, RuleDetail, RuleResultPagedResults } from '../models/rule.model';
import { JsonResult } from '../models/jsonresult.model';
import { SortOrder } from '../models/enums.model';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';

@Injectable({
    providedIn: 'root'
})
export class RulesService extends BaseObservableService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    getRule(id: number): Observable<RuleDetail> {
        return this.http.get(`api/rule/${id}`)
            .pipe(
                map(response => <RuleDetail>response),
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

    deleteRuleType(id: number): Observable<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'ruletype', id);
    }

    saveRuleType(ruleType: RuleType): Observable<JsonResult> {
        if (ruleType.ID == undefined || !ruleType.ID) {
            return this.postDynamic(this.http, 'ruletype', ruleType);
        }
        return this.putDynamic(this.http, 'ruletype', ruleType);
    }

    getResultsByRule(uid: string,
        pageNumber?: number,
        pageSize?: number,
        sortField?: string,
        sortOrder?: SortOrder,
        isExport: boolean = false,
        ruleId?: number,
        simpleFilter: string = "",
        advancedFilter: string = ""
    ): Observable<RuleResultPagedResults> {
        let sortOrderText = sortOrder == SortOrder.None ? "desc" : (sortOrder == SortOrder.Descending ? "desc" : "asc");
        let uri = `api/v2/metrics/quality/results?_owningAssetUid=${uid}`

        let fileName = " Rule Results"

        if (sortField) {
            uri += "&_order=" + sortField
            if (sortOrder && sortOrderText != "") {
                uri += "&_direction=" + sortOrderText
            }
        }
        if (simpleFilter) {
            uri += "&_simpleFilter=" + simpleFilter;
        }
        if (advancedFilter) {
            uri += "&_filter=" + advancedFilter;
        }

        if (isExport) {
            // get Friendly name export
            if (!pageSize) {
                pageSize = 20000;
            }
            uri += `&_isFriendlyNameExport=true&_pageNum=1&_pageSize=${pageSize}&_includeDuplicateFlag=True`;

            this.getRule(ruleId)
                .subscribe(result => {
                    fileName = result.Name + fileName;
                });
            this.
                http
                .get(uri, { headers: new HttpHeaders({ 'Accept': 'application/octet-stream' }), responseType: 'blob' })
                .subscribe(
                    data => this.downloadFile(data, fileName)
                );
        } else {
            if (pageSize) {
                uri += "&_pageSize=" + pageSize
            }
            if (pageNumber) {
                uri += "&_pageNum=" + (pageNumber + 1)
            }
            uri += "&_includeDuplicateFlag=True";

            return this.http.get(uri)
                .pipe(
                    map(response => <RuleResultPagedResults>response),
                    catchError(err => this.handleError(err))
                );
        }

    }

    downloadFile(data: any, name: string = 'Rule Results') {

        var filename = `${name} ${new Date().toDateString()}.xlsx`;
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

    hasCustomExport(uid: string): Observable<boolean> {
        return this.http.get(`api/v2/exporttemplates/hasCustomExport/${uid}`).pipe(
            response => response,
            catchError(err => this.handleError(err))
        );
    }
}