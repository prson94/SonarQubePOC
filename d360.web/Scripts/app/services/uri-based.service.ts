
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { ObjectStyle } from '../models/object-detail.model';
import { JsonResult } from '../models/jsonresult.model';

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
            .post(uri, 'json=' + JSON.stringify(item), { headers: headers })
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(this.handleError);
    }

    private put(uri: string, item: any): Promise<JsonResult> {
        let headers = new Headers({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8' //pass as text since its a dynamic object and mvc has issue with dynamic models
        });
        
        return this.http
            .put(uri, 'json=' + JSON.stringify(item), { headers: headers })
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(this.handleError);
    }
}