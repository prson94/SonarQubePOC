///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { ObjectStyle } from '../models/object-detail.model';

@Injectable()
export class UriBasedService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getItems(uri: string): Promise<any[]> {
        return this.http.get(uri)
            .toPromise()
            .then(response => <any[]>response.json())
            .catch(err => this.handleError(err));
    }

    //assumes delete url ends with id of item to delete...
    deleteItem(uri: string, id: number) {
        let headers = new Headers();
        headers.append('Content-Type', 'application/json');
                
        return this.http
            .delete(`${uri}${id}`, headers)
            .toPromise()
            .catch(err => this.handleError(err));
    }
}