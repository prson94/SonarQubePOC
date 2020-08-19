import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from "rxjs";
import { catchError, map } from "rxjs/operators";

import { BaseObservableService } from "./baseObservable.service";
import { MessagesObservableService } from './messages-observable.service';
import { ConnectorLabel } from '../models/connectorLabel.model';


@Injectable()
export class ConnectorLabelService extends BaseObservableService {

    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) {
        super(messagesService);
    }


    public getAvailableLabels(q: string, isExactValue: boolean = false, getUseCount: boolean = false, excludeUid: number = null): Observable<any[]> {
        let url = `/api/v2/connectorLabels/search?q=` + q;
        if (isExactValue) {
            url = url + '&isExact=true';
        }
        if (getUseCount) {
            url = url + '&getUseCount=true';
        }
        if (excludeUid) {
            url = url + '&exceptUid=' + excludeUid;
        }
        return this
            .http
            .get(url)
            .pipe(
                map(response => <any[]>response),
                catchError(err => this.handleError(err, true))
            );
    }

    public createOrGetLabel(q: string): Observable<any> {
        return this
            .http
            .post(`/api/v2/connectorLabels/insertOrGet`, { Value: q })
            .pipe(
                map(response => <any>response),
                catchError(err => this.handleError(err, true))
            );
    }

    getLabelList(getAll: boolean = true): Observable<ConnectorLabel[]> {
        let url = `api/v2/connectorLabels/`;

        if (getAll) {
            url += "?_pageNum=1&_pageSize=10000";
        }

        return this.http.get(url)
            .pipe(map(response => <any>response),
                map(items => <ConnectorLabel[]>items.items),
                catchError(err => this.handleError(err)));

    }

    getLabelUsage(labelUid: string): Observable<any[]> {
        let url = `api/v2/connectorLabels/` + labelUid + `/usage`;

        return this.http.get(url)
            .pipe(map(response => <any>response),
                map(items => <any[]>items),
                catchError(err => this.handleError(err)));

    }


    exportLabelUsage(labelUid: string, fileName: string) {
        let url = `api/v2/connectorLabels/` + labelUid + `/usage`;
        this.
            http
            .get(url, { headers: new HttpHeaders({ 'Accept': 'application/octet-stream' }), responseType: 'blob' })
            .subscribe(data => this.downloadFile(data, fileName));

    }

    deleteLabelByUid(uid: string, cascade: boolean = true): Observable<any> {
        let url = `api/v2/connectorLabels/${uid}?cascade=${cascade}`;
        return this.http.delete(url)
            .pipe(map(response => <any>response),
                catchError(err => this.handleError(err, true)));
    }

    deleteLabels(labels: ConnectorLabel[]): Observable<any> {
        let url = `api/v2/connectorLabels/`;

        if (labels.length == 1)
            return this.deleteLabelByUid(labels[0].uid);

        let body: any[] = []
        labels.forEach(t => {
            body.push({ 'uid': t.uid, cascade: true });
        })

        const httpHeaders = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' }), body: body
        };
        return this.http.delete(url, httpHeaders)
            .pipe(map(response => <any>response),
                catchError(err => this.handleError(err, true)));
    }

    saveLabel(label: ConnectorLabel): Observable<any> {
        let url = `api/v2/connectorLabels/`;

        if (label.uid == undefined || !label.uid) {
            return this.http.post(url, label)
                .pipe(map(response => <any>response),
                    catchError(err => this.handleError(err, true)));
        }
        url = `api/v2/connectorLabels/${label.uid}`;
        return this.http.put(url, label)
            .pipe(map(response => <any>response),
                catchError(err => this.handleError(err, true)));
    }

    consolidateTags(parentLabel: string, childrenLabels: string[]): Observable<any[]> {
        let url = `api/v2/connectorLabels/consolidate/${parentLabel}`;
        return this.http.post(url, childrenLabels)
            .pipe(map(response => <any>response),
                catchError(err => this.handleError(err, true)));
    }

    exportLabels(filters: any, sort) {
        this.http.get(`api/v2/connectorLabels/export?globalSearch=${filters.globalSearch}&value=${filters.Value}&useCount=${filters.UseCount}&sortBy=${sort.field}&sortOrder=${sort.order}`, { responseType: 'blob' }).subscribe(data => this.downloadFile(data, 'Connector Labels'));
    }

    downloadFile(data: Blob, name: string) {
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


}
