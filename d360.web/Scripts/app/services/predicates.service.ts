///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { Predicate } from '../models/predicate.model';
import { JsonResult } from '../models/jsonresult.model';

@Injectable()
export class PredicatesService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getPredicates(): Promise<Predicate[]> {
        return this.http.get(`relations/predicates`)
            .toPromise()
            .then(response => <Predicate[]>response.json())
            .catch(err => this.handleError(err));
    }

    deletePredicate(id: number) {
        this.deleteDynamic(this.http, 'predicate', id);
    }

    savePredicate(predicate: Predicate): Promise<JsonResult> {
        if (predicate.ID == undefined || !predicate.ID) {
            return this.postDynamic(this.http, 'predicate', predicate);
        }
        return this.putDynamic(this.http, 'predicate', predicate);
    }
}