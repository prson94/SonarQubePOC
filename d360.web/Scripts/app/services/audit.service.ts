import {Injectable} from '@angular/core';
import {HttpClient, HttpHeaders} from "@angular/common/http";
import {Observable} from "rxjs";
import {catchError, map} from "rxjs/operators";

import { AuditResults, AuditObject, AuditFilterLists } from "../models/audit.model";

import {BaseObservableService} from './baseObservable.service';
import {MessagesObservableService} from './messages-observable.service';

@Injectable({
    providedIn: 'root'
})
export class AuditService extends BaseObservableService {
    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) {
        super(messagesService);
    }

    public getAuditData(assetUid: string, params: any): Observable<AuditResults> {
        var qString = '';
        if (params) {
            qString = Object.keys(params).map((key) => key + '=' + params[`${key}`]).join('&');
            if (qString) {
                qString = '?' + qString;
            }
        }

        return this
            .http
            .get(`/api/v2/audit/${assetUid}${qString}`)
            .pipe(
                map((response) => <AuditResults>response),
                catchError((err) => this.handleError(err))
            );
    }

    public getLegacyDetails(assetUid: string): Observable<AuditObject> {
        return this
            .http
            .get(`/api/v2/audit/objectdetail/${assetUid}`)
            .pipe(
                map((response) => <AuditObject>response),
                catchError((err) => this.handleError(err))
            );
    }

    public getFilterLists(assetUid: string): Observable<AuditFilterLists> {
        return this
            .http
            .get(`/api/v2/audit/filterlists/${assetUid}`)
            .pipe(
                map((response) => <AuditFilterLists>response),
                catchError((err) => this.handleError(err))
            );
    }

    public exportToExcel(assetUid: string, params: any, fileName) {

        //Setup paging for export
        params['_pageNum'] = 1;
        params['_pageSize'] = 200000;

        var qString = '';
        if (params) {
            qString = Object.keys(params).map((key) => key + '=' + params[`${key}`]).join('&');
            if (qString) {
                qString = '?' + qString;
            }
        }
        this.
            http
            .get(`/api/v2/audit/${assetUid}${qString}`, { headers: new HttpHeaders({ 'Accept': 'application/octet-stream' }), responseType: 'blob' })
            .subscribe((data) => this.downloadFile(data, fileName));
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
