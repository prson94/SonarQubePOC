
import {catchError, map} from 'rxjs/operators';
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { Tag } from '../models/tag.model';
import { Observable } from 'rxjs';

@Injectable()
export class TagService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getTags(phrase: string, excludeObjects: string = ''): Observable<Tag[]> {
        let url = `api/tagsuggestions?phrase=${phrase}&excludeObjects=${excludeObjects}`;
        
        return this.http.get(url).pipe(
            map(response => {
                    return response.json()}),
            map(item => { return <Tag[]> item}),
            catchError(err => this.handleError(err)),);
    }
}