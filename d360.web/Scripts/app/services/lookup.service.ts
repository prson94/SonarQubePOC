import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { Lookup, LookupItem } from '../models/lookup.model';
import { JsonResult } from '../models/jsonresult.model';

@Injectable()
export class LookupService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getLookups(): Promise<Lookup[]> {
        return this.http.get('resources/_Lookups')
            .toPromise()
            .then(response => <Lookup[]>response.json().results)
            .catch(err => this.handleError(err));
    }

    deleteLookup(lookupId: number): Promise<JsonResult> {
        let headers = new Headers();
        headers.append('Content-Type', 'application/json');

        let url = `form/LookupType/${lookupId}`;

        return this.http
            .delete(url, headers)            
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(err => this.handleError(err));
    }

    saveLookup(lookup: Lookup): Promise<JsonResult> {
        if (lookup.ID == undefined || !lookup.ID) {
            return this.post(lookup);
        }
        return this.put(lookup);    
    }

    private post(lookup: Lookup): Promise<JsonResult> {
        let headers = new Headers({
            'Content-Type': 'application/json'
        });
        return this.http
            .post("form/AddLookupTypeRaw", JSON.stringify(lookup), { headers: headers })
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(err => this.handleError(err));
    }

    private put(lookup: Lookup): Promise<JsonResult> {
        let headers = new Headers({
            'Content-Type': 'application/json'
        });
        
        return this.http
            .put('form/EditLookupTypeRaw', JSON.stringify(lookup), { headers: headers })
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(err => this.handleError(err));
    }
    
}