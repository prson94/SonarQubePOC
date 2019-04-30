import {Injectable} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {Observable} from "rxjs";
import {catchError, map} from "rxjs/operators";

import {AuditResults} from '../models/audit.model';
import {SortOrder} from '../models/enums.model';
import {GridFilterExpression} from '../models/grid-definition.model';

import {BaseObservableService} from './baseObservable.service';
import {MessagesObservableService} from './messages-observable.service';

@Injectable()
export class AuditService extends BaseObservableService {
    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) {
        super(messagesService);
    }

    getAuditData(
        objectID: number,
        objectType: string,
        pageNum: number,
        pageSize: number,
        sortOrder: SortOrder,
        sortField?: string,
        filters?: GridFilterExpression[]
    ): Observable<AuditResults> {
        let sortCol = sortField != undefined ? sortField : "";
        console.log(sortOrder);
        let url = `api/v2/audit/${objectType}/${objectID}/auditcombined.json?pagenum=${pageNum}&pagesize=${pageSize}&sortdatafield=${sortField}&sortorder=${sortOrder == SortOrder.None ? "" : (sortOrder == SortOrder.Ascending ? "asc" : "desc")}`;
        let indx = 0;

        if (filters != undefined) {
            url += `&filterscount=${filters.length}`;

            for (let filter of filters) {
                url += `&filtervalue${indx}=${filter.value}&filtercondition${indx}=${filter.condition}&filteroperator${indx}=1&filterdatafield${indx}=${filter.field}`;
                indx++;
            }
        }

        return this
            .http
            .get(url)
            .pipe(
                map(response => <AuditResults>response),
                catchError(err => this.handleError(err))
            );
    }

    exportToExcel(objectID: number, objectType: string, name: string, filters?: GridFilterExpression[]) {        
        let url = `api/v2/audit/${objectType}/${objectID}/download/excel/audit.xls`;

        let indx = 0;

        if (filters != undefined) {
            url += `?filterscount=${filters.length}`;

            for (let filter of filters) {
                url += `&filtervalue${indx}=${filter.value}&filtercondition${indx}=${filter.condition}&filteroperator${indx}=1&filterdatafield${indx}=${filter.field}`;
                indx++;
            }
        }

        this
            .http
            .get(
                url,
                {responseType: 'blob'}
            )
            .subscribe(data => this.downloadFile(data, name));
    }

    downloadFile(
        data: Blob,
        name: string
    ) {
        var filename = `${name} Audit Data ${new Date().toDateString()}.xlsx`;

        if (window.navigator.msSaveOrOpenBlob) {
            window.navigator.msSaveOrOpenBlob(data, filename);
        } else {
            var url = window.URL.createObjectURL(data);
            var anchor = document.createElement("a");

            anchor.setAttribute("style", "display:none;");
            document.body.appendChild(anchor);
            anchor.setAttribute("download", filename);
            anchor.href = url;
            anchor.click();
        }
    }
}
