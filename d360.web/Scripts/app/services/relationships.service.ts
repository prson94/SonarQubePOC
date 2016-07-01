///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { Relationship } from '../models/relationship.model';

@Injectable()
export class RelationshipsService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getRelations(): Promise<Relationship[]> {
        return this.http.get('relations/_intersectTypes')
            .toPromise()
            .then(response => <Relationship[]>response.json())
            .catch(err => this.handleError(err));
    }
}