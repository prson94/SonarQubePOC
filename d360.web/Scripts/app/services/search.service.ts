import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { SearchResultsObject, SearchCategories, AdvancedSearchFilter } from '../models/search-result.model';

@Injectable()
export class SearchService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getSearchResults(term: string, size: number, pageNum: number, searchTypes: string[], category?: SearchCategories, isExactMatch?: boolean, advancedSearchFilter?: AdvancedSearchFilter[]): Promise<SearchResultsObject> {

        term = (isExactMatch ? `'${term}'` : term);
        
        let headers = new Headers({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8', //pass as text since its a dynamic object and mvc has issue with dynamic models                        
        });
        
        let url = '';

        if (category && category.Categories)
            url = `from=${pageNum}&size=${size}&search=${advancedSearchFilter ? '' : encodeURIComponent(term)}&group=&type=${category.Name}&adv=${advancedSearchFilter ? encodeURIComponent(JSON.stringify(advancedSearchFilter)) : ''}`;
        else
            url = `from=${pageNum}&size=${size}&search=${advancedSearchFilter ? '' : encodeURIComponent(term)}&group=${category && !category.DisplayName ? category.Name : ''}&type=${searchTypes ? searchTypes.join(',') : ''}&adv=${advancedSearchFilter ? encodeURIComponent(JSON.stringify(advancedSearchFilter)) : ''}`;

       return this.http
            .post('search/results', url, { headers: headers })
            .toPromise()
            .then(res => <SearchResultsObject>res.json())
            .catch(err => this.handleError(err));
    }
}