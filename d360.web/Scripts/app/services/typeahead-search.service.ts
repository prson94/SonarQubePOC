import { catchError, map, debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { Injectable } from '@angular/core';
import { SearchResult } from '../models/search-result.model';
import { Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';

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
                let uri = `search/typeahead?q=${encodeURIComponent(term.substring(0, 255))}&num=${size}&t=${types != undefined ? types.join(',') : ''}`;
                return this.http.get(uri).pipe(
                    map(response => <SearchResult[]>response),
                    catchError(err => this.handleError(err))
                );
            }));
    }

    getObjectItems(size: number, term: string, objectType: string, objectId: number) {
        return this.http.get(`api/breadcrumb/typeahead?q=${term}&num=${size}&objectType=${objectType}&objectId=${objectId}`)
            .pipe(
                map(response => <SearchResult[]>response),
                catchError(err => this.handleError(err))
            );
    }
    getObjectTypeItemsFromParent(size: number, term: string, objectType: string, objectId: number) {
        return this.http.get(`api/breadcrumb/typeaheadfortype?q=${term}&num=${size}&objectType=${objectType}&objectId=${objectId}`)
            .pipe(
                map(response => <SearchResult[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getObjectTypeItems(size: number, term: string, objectType: string) {
        return this.http.get(`api/breadcrumb/typeaheadfortypewithoutparent?q=${term}&num=${size}&objectType=${objectType}`)
            .pipe(
                map(response => <SearchResult[]>response),
                catchError(err => this.handleError(err))
            );
    }    
}