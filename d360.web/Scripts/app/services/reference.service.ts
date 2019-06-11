import { Injectable } from '@angular/core';
import { HttpClient, HttpRequest, HttpResponse } from '@angular/common/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { ReferenceItemType, ReferenceItem } from '../models/reference.model';
import { JsonResult } from '../models/jsonresult.model';
import { Observable } from 'rxjs';
import { map, catchError } from 'rxjs/operators';

@Injectable()
export class ReferenceService extends BaseService {

    constructor(private http: HttpClient, messagesService: MessagesService) { super(messagesService); }

    getReferenceItemTypes(): Observable<ReferenceItemType[]> {
        return this.http.get(`api/referenceItemTypes`)
            .pipe(
                map(response => <ReferenceItemType[]>response),
                catchError(err => this.handleError(err)));
    }

    canReadReferenceType(id: number): Observable<boolean> {
        return this.http.get(`api/canReadReferenceItemType/${id}`)
            .pipe(
             map(response => <boolean>response),
            catchError(err => this.handleError(err)));
    };

    saveReferenceItemType(item: ReferenceItemType) {
        if (item.ID == undefined || !item.ID) {
            return this.postDynamicObs(this.http, 'referenceItemType', item);
        }
        return this.putDynamicObs(this.http, 'referenceItemType', item);
    }

    deleteReferenceItemType(id: number): Observable<JsonResult> {
        return this.deleteDynamicWithResultObs(this.http, 'referenceItemType', id);
    }

    exportReferenceItems(id: number, name: string) {
        this.http.get(`api/referenceItems/${id}/items.xls`, { responseType : 'blob' }).subscribe(data => this.downloadFile(data, name));
    }

    downloadFile(data: Blob, name: string) {
        var filename = `${name} List ${new Date().toDateString()}.xlsx`;
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