///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { SearchResultsObject } from '../models/search-result.model';

@Injectable()
export class SearchService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getSearchResults(term: string, size: number, pageNum: number): Promise<SearchResultsObject> {
        let headers = new Headers({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8', //pass as text since its a dynamic object and mvc has issue with dynamic models                        
        });

        this.addRequestVerificationHeaders(headers);
        
        return this.http
            .post('search/results', `from=${pageNum}&size=${size}&search=${term}&group=&type=Artifact&adv=`, { headers: headers })
            .toPromise()
            .then(res => <SearchResultsObject>res.json())
            .catch(this.handleError);
    }
}