
import {catchError, map} from 'rxjs/operators';
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { SearchResult } from '../models/search-result.model';
import { Observable } from 'rxjs';

@Injectable()
export class TypeaheadSearchService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getResults(size, term, types?: string[]): Observable<SearchResult[]> {
        
        return this.http.get(`search/typeahead?q=${encodeURIComponent(term)}&num=${size}&t=${types != undefined ? types.join(',') : ''}`).pipe(
            map(response => {
                return response.json()
                    .map(item => { return <SearchResult[]>item })
            }),
            catchError(err => this.handleError(err)),);
    }

    getObjectTypeItems(size: number, term: string, objectType: string, objectId: number) {
        return this.http.get(`api/breadcrumb/typeahead?q=${term}&num=${size}&objectType=${objectType}&objectId=${objectId}`).pipe(
            map(response => {
                return response.json().map(item =>
                        { return <SearchResult[]>item; }
                   )
            }),
            catchError(err => this.handleError(err)),);
            
    }  
    getObjectItems(size: number, term: string, objectType: string, objectId: number) {
        return this.http.get(`api/breadcrumb/typeaheadfortype?q=${term}&num=${size}&objectType=${objectType}&objectId=${objectId}`).pipe(
            map(response => {
                return response.json().map(item => { return <SearchResult[]>item; }
                )
            }),
            catchError(err => this.handleError(err)));

    }  
}