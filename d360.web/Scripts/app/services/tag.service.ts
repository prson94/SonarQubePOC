import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { Tag } from '../models/tag.model';
import { Observable } from 'rxjs/Observable';

@Injectable()
export class TagService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getTags(phrase: string): Observable<Tag[]> {
        return this.http.get(`api/tagsuggestions?phrase=${phrase}`)
            .map(response => {
                return <Tag[]> response.json()})
            .catch(err => this.handleError(err));
    }
}