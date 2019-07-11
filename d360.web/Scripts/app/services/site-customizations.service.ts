import { Injectable } from '@angular/core';
import { JsonResult } from '../models/jsonresult.model';
import { HttpClient } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';

@Injectable()
export class SiteCustomizationsService extends BaseObservableService  {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    getCustomCss(): Observable<string> {
        return this.http.get('/form/stylecustomizations')
            .pipe(
                map(response => <string>response),
                catchError(err => this.handleError(err))
            );
    }

    saveCustomCss(css: string): Observable<JsonResult> {
        return this.http.put('form/UpdateStyleCustomizations', { css: css })
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }
}