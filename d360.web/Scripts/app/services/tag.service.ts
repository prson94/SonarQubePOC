
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { Tag } from '../models/tag.model';

@Injectable()
export class TagService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getTags(phrase: string): Promise<Tag[]> {
        return this.http.get(`api/tagsuggestions?phrase=${phrase}`)
            .toPromise()
            .then(response => <Tag[]>response.json())
            .catch(err => this.handleError(err));
    }
}