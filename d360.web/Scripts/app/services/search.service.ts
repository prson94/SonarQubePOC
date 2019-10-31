import { Injectable } from '@angular/core';
import { SearchResultsObject, SearchCategories, AdvancedSearchFilter, SearchQuery, SearchAggregationFilter, SearchFieldFilter } from '../models/search-result.model';
import { HttpClient } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';

@Injectable()
export class SearchService extends BaseObservableService  {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    getSearchResultsByQuery(query: SearchQuery): Observable<SearchResultsObject> {
        return this.http
            .post('search/results', query)
            .pipe(
                map(res => <SearchResultsObject>res),
                catchError(err => this.handleError(err))
            );
    }
}