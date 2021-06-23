import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from "@angular/common/http";
import { MessagesObservableService } from './messages-observable.service';
import { BaseObservableService } from "./baseObservable.service";
import { Observable } from "rxjs";
import { catchError, map } from "rxjs/operators";
import { Predicate, PredicateType } from '../models/predicate.model';
import { JsonResult } from '../models/jsonresult.model';
import { ApiResult } from '../models/apiresult.model';

@Injectable({
    providedIn: 'root'
})
export class PredicatesService extends BaseObservableService {

    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) { super(messagesService); }

    getPredicates(): Observable<Predicate[]> {
        return this
            .http
            .get('/api/v2/relationships/predicates')
            .pipe(
                map(response => <Predicate[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getPredicatesByType(type: PredicateType): Observable<Predicate[]> {
        return this
            .http
            .get('/api/v2/relationships/predicates?Type=' + type)
            .pipe(
                map(response => <Predicate[]>response),
                catchError(err => this.handleError(err))
            );
    }

    deletePredicate(uid: string): Observable<ApiResult[]> {
        var model = [];
        model.push({ Uid: uid });
        const httpHeaders = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' }), body: model
        };
        return this.http.delete(`/api/v2/relationships/predicates`, httpHeaders).pipe(
            map(response => response),
            catchError(err => this.handleError(err, true))
        );
    }

    savePredicate(predicate: Predicate): Observable<ApiResult[]> {
        let model: any[] = [];
        model.push(predicate);
        return this.http.post(`/api/v2/relationships/predicates`, model).pipe(
            map(response => response),
            catchError(err => this.handleError(err, true))
        );
    }
}