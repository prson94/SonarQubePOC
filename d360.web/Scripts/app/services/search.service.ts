import { Injectable } from '@angular/core';
import { SearchResultsObject, SearchCategories, AdvancedSearchFilter, SearchQuery, SearchAggregationFilter, SearchFieldFilter } from '../models/search-result.model';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';

@Injectable()
export class SearchService extends BaseObservableService  {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    getSearchResults(term: string, size: number, fromNum: number, searchTypes: string[], category?: SearchCategories, isExactMatch?: boolean, advancedSearchFilter?: AdvancedSearchFilter[]): Observable<SearchResultsObject> {

        var query = new SearchQuery({
            Term: (isExactMatch ? `'${term}'` : term),
            From: fromNum,
            Size: size,
            AggregationFilters: [],
            FieldFilters: [],
            Aggregations: []
        });
        if (category && category.Categories) {
            query.AggregationFilters.push( new SearchAggregationFilter({
                Field: 'd3sGroup',
                Values: [category.Name]
            }));
        } else {
            query.Aggregations.push('category');
            if (searchTypes && searchTypes.length > 0)
                query.AggregationFilters.push(new SearchAggregationFilter({
                    Field: 'd3sGroup',
                    Values: searchTypes
                }));
            if(category && !category.DisplayName)
                query.AggregationFilters.push(new SearchAggregationFilter({
                    Field: 'Type',
                    Values: [category.Name]
                }));
        }
        if (advancedSearchFilter) {
            advancedSearchFilter.forEach(function (item) {
                query.FieldFilters.push(new SearchFieldFilter({
                    Field: item.field,
                    Phrase: item.value,
                    MatchWords: item.exact
                }));
            });
        }

        return this.http
            .post('search/NewResults', query)
            .pipe(
                map(res => <SearchResultsObject>res),
                catchError(err => this.handleError(err))
            );
    }
}