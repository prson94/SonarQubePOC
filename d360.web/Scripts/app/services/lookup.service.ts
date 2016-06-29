///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { Lookup, LookupItem } from '../models/lookup.model';

@Injectable()
export class LookupService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getLookups(): Promise<Lookup[]> {
        return this.http.get('resources/_Lookups')
            .toPromise()
            .then(response => <Lookup[]>response.json().results)
            .catch(err => this.handleError(err));
    }

    deleteLookup(lookupId: number) {

    }

    getLookupItems(lookup: Lookup): Promise<LookupItem[]> {
        return this.http.get(`resources/lookups/${lookup.ID}/items.json`)
            .toPromise()
            .then(response => <LookupItem[]>response.json())
            .catch(err => this.handleError(err));
    }
}