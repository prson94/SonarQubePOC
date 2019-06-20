import { Injectable } from '@angular/core';
import { HttpClient } from "@angular/common/http";
import { MessagesObservableService } from './messages-observable.service';
import { BaseObservableService } from "./baseObservable.service";
import { Observable } from "rxjs";
import { catchError, map } from "rxjs/operators";
import { Predicate } from '../models/predicate.model';
import { JsonResult } from '../models/jsonresult.model';

@Injectable()
export class PredicatesService extends BaseObservableService {

    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) { super(messagesService); }

    getPredicates(): Observable<Predicate[]> {
        return this
            .http
            .get('relations/predicates')
            .pipe(
                map(response => <Predicate[]>response),
                catchError(err => this.handleError(err))
            );
    }

    deletePredicate(id: number): Observable<JsonResult> {        
        return this.deleteDynamicWithResult(this.http, 'predicate', id);
    }

    savePredicate(predicate: Predicate): Observable<JsonResult> {
        if (predicate.ID == undefined || !predicate.ID) {
            return this.postDynamic(this.http, 'predicate', predicate);
        }
        return this.putDynamic(this.http, 'predicate', predicate);
    }
}