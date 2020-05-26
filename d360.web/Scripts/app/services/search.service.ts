import { Injectable } from '@angular/core';
import { SearchResultsObject, SearchQuery, SearchResultInfo } from '../models/search-result.model';
import { HttpClient } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { Observable, of } from 'rxjs';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';

@Injectable()
export class SearchService extends BaseObservableService  {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    private getEmptyResult(): SearchResultsObject {
        let result = new SearchResultsObject();
        result.Categories = [];
        result.Result = new SearchResultInfo();
        result.Result.Results = [];
        result.Result.ElapsedMS = 0;
        result.Result.Matches = 0;
        return result;
    }

    getSearchResultsByQuery(query: SearchQuery): Observable<SearchResultsObject> {
        if (query.Term == undefined || query.Term == "") {
            //No search term, no results, no need to call endpoint
            return of(this.getEmptyResult());
        }

        return this.http
            .post('search/results', query)
            .pipe(
                map(res => <SearchResultsObject>res),
                catchError(err => this.handleError(err))
            );
    }
}