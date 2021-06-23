import { Injectable } from '@angular/core';
import { HttpClient, HttpRequest, HttpResponse } from '@angular/common/http';
import { ReferenceItemType, ReferenceItem } from '../models/reference.model';
import { JsonResult } from '../models/jsonresult.model';
import { Observable } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';

@Injectable({
    providedIn: 'root'
})
export class ReferenceService extends BaseObservableService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    canReadReferenceType(id: number): Observable<boolean> {
        return this.http.get(`api/canReadReferenceItemType/${id}`)
            .pipe(
             map(response => <boolean>response),
            catchError(err => this.handleError(err)));
    }

    saveReferenceItemType(item: ReferenceItemType) {
        if (item.ID == undefined || !item.ID) {
            return this.postDynamic(this.http, 'referenceItemType', item);
        }
        return this.postDynamic(this.http, 'referenceItemType', item);
    }

    deleteReferenceItemType(id: number): Observable<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'referenceItemType', id);
    }
}