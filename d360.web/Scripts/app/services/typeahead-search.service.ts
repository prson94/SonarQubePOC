import { catchError, map, debounceTime, distinctUntilChanged, switchMap } from "rxjs/operators";
import { Injectable } from '@angular/core';
import { SearchResult } from '../models/search-result.model';
import { Observable, of } from "rxjs";
import { HttpClient, HttpContext } from '@angular/common/http';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';
import { ROUTE_INDEPENDENT_QUERY } from "../http-interceptors";

@Injectable({
    providedIn: 'root'
})
export class TypeaheadSearchService extends BaseObservableService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    getResults(term: Observable<string>, size, types?: string[]): Observable<SearchResult[]> {
        return term.pipe(
            debounceTime(400),
            distinctUntilChanged(),
            switchMap(term => {
                if (term === "") {
                    return of(<SearchResult[]>[]);
                }
                let uri = `api/v2/search/typeahead?q=${encodeURIComponent(term.substring(0, 255))}&num=${size}&t=${typeof types !== "undefined" ? types.join(',') : ''}`;
                return this.http.get(
                    uri,
                    { context: new HttpContext().set(ROUTE_INDEPENDENT_QUERY, true) }
                ).pipe(
                    map(response => <SearchResult[]>response),
                    catchError(err => this.handleError(err))
                );
            }));
    }

    getObjectItems(size: number, term: string, objectType: string, objectId: number) {
        return this.http
            .get(
                `api/breadcrumb/typeahead?q=${term}&num=${size}&objectType=${objectType}&objectId=${objectId}`,
                { context: new HttpContext().set(ROUTE_INDEPENDENT_QUERY, true) }
            )
            .pipe(
                map(response => <SearchResult[]>response),
                catchError(err => this.handleError(err))
            );
    }
    getObjectTypeItemsFromParent(size: number, term: string, objectType: string, objectId: number) {
        return this.http
            .get(
                `api/breadcrumb/typeaheadfortype?q=${term}&num=${size}&objectType=${objectType}&objectId=${objectId}`,
                { context: new HttpContext().set(ROUTE_INDEPENDENT_QUERY, true) }
            )
            .pipe(
                map(response => <SearchResult[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getObjectTypeItems(size: number, term: string, objectType: string) {
        return this.http
            .get(
                `api/breadcrumb/typeaheadfortypewithoutparent?q=${term}&num=${size}&objectType=${objectType}`,
                { context: new HttpContext().set(ROUTE_INDEPENDENT_QUERY, true) }
            )
            .pipe(
                map(response => <SearchResult[]>response),
                catchError(err => this.handleError(err))
            );
    }
}
