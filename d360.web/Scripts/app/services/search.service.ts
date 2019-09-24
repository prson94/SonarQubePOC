import { Injectable } from '@angular/core';
import { SearchResultsObject, SearchCategories, AdvancedSearchFilter } from '../models/search-result.model';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';

@Injectable()
export class SearchService extends BaseObservableService  {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    getSearchResults(term: string, size: number, fromNum: number, searchTypes: string[], category?: SearchCategories, isExactMatch?: boolean, advancedSearchFilter?: AdvancedSearchFilter[]): Observable<SearchResultsObject> {

        term = (isExactMatch ? `'${term}'` : term);

        let headers = new HttpHeaders({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8', //pass as text since its a dynamic object and mvc has issue with dynamic models                        
        });
        
        let url = '';

        if (category && category.Categories)
            url = `from=${fromNum}&size=${size}&search=${advancedSearchFilter ? '' : encodeURIComponent(term)}&group=&type=${category.Name}&adv=${advancedSearchFilter ? encodeURIComponent(JSON.stringify(advancedSearchFilter)) : ''}`;
        else
            url = `from=${fromNum}&size=${size}&search=${advancedSearchFilter ? '' : encodeURIComponent(term)}&group=${category && !category.DisplayName ? category.Name : ''}&type=${searchTypes ? searchTypes.join(',') : ''}&adv=${advancedSearchFilter ? encodeURIComponent(JSON.stringify(advancedSearchFilter)) : ''}`;

       return this.http
            .post('search/results', url, { headers })
            .pipe(
                map(res => <SearchResultsObject>res),
                catchError(err => this.handleError(err))
            );
    }
}