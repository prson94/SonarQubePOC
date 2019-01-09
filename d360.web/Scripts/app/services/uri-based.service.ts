
import {switchMap, distinctUntilChanged, debounceTime, map} from 'rxjs/operators';
import { Injectable } from '@angular/core';
import { Headers, Http, RequestOptions } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { ObjectStyle } from '../models/object-style.model';
import { JsonResult } from '../models/jsonresult.model';
import { Observable } from 'rxjs';

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

        let options = new RequestOptions({ headers: headers });
                
        return this.http
            .delete(`${uri}${id}`, options)
            .toPromise()
            .catch(err => this.handleError(err));
    }

    deleteItemWithResult(uri: string, id: number): Promise<JsonResult> {
        let headers = new Headers();
        headers.append('Content-Type', 'application/json');

        let options = new RequestOptions({ headers: headers });

        return this.http
            .delete(`${uri}${id}`, options)
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(err => this.handleError(err));
    }

    saveItem(createUri: string, editUri: string, item: any): Promise<JsonResult> {
        if (item.ID == undefined || !item.ID) {
            return this.post(createUri, item);
        }
        return this.put(editUri, item);       
    }

    private post(uri: string, item: any): Promise<JsonResult> {
        let headers = new Headers({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8' //pass as text since its a dynamic object and mvc has issue with dynamic models
        });
                
        return this.http
            .post(uri, 'json=' + encodeURIComponent(JSON.stringify(item)), { headers: headers })
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(err => this.handleError(err));
    }

    private put(uri: string, item: any): Promise<JsonResult> {
        let headers = new Headers({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8' //pass as text since its a dynamic object and mvc has issue with dynamic models
        });
        
        return this.http
            .put(uri, 'json=' + encodeURIComponent(JSON.stringify(item)), { headers: headers })
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(err => this.handleError(err));
    }

    getAsObservable(uri: string) {
        return this.http.get(uri).pipe(map(res => res.json()));
    }

    search(uri: string, query: Observable<string>, debounceTimeParametr: number = 300, emptyResults: boolean = false) {
        return query.pipe(debounceTime(debounceTimeParametr),
            distinctUntilChanged(),
            switchMap(query => this.getAsObservable(uri + query)),);
    }
}