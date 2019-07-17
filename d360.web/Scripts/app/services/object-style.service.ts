import { Injectable } from '@angular/core';
import { ObjectStyle } from '../models/object-style.model';
import { BaseObservableService } from "./baseObservable.service";
import { MessagesObservableService } from './messages-observable.service';
import {HttpClient} from "@angular/common/http";
import {Observable} from "rxjs";
import {catchError, map} from "rxjs/operators";

@Injectable()
export class ObjectStyleService extends BaseObservableService {
    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) {
        super(messagesService);
    }

    getObjectStyle(
        objectID: number,
        objectType: string
    ): Observable<ObjectStyle> {
        return this.http.get(`api/${objectType}/${objectID}/style`).pipe(
            map(response => <ObjectStyle>response),
            catchError(err => this.handleError(err))
        );
    }
}
