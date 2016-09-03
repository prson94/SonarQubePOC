///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { SearchResult } from '../models/search-result.model';

@Injectable()
export class TypeaheadSearchService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getResults(size, term): Promise<SearchResult[]> {
        return this.http.get(`search/typeahead?q=${term}&num=${size}&t=`)
            .toPromise()
            .then(response => <SearchResult[]>response.json())
            .catch(err => this.handleError(err));
    }

    getObjectTypeItems(size: number, term: string, objectType: string, objectId: number) {
        return this.http.get(`api/breadcrumb/typeahead?q=${term}&num=${size}&objectType=${objectType}&objectId=${objectId}`)
            .toPromise()
            .then(response => <SearchResult[]>response.json())
            .catch(err => this.handleError(err));
    }
}