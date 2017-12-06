import { Injectable } from '@angular/core';
import { Headers, Http, RequestOptions } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { JsonResult } from '../models/jsonresult.model';


@Injectable()
export class LevelsService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getObjectLevels(objectID: number, objectType: string): Promise<any[]> {
        return this.http.get(`api/${objectType}/${objectID}/levels`)
            .toPromise()
            .then(response => <any[]>response.json())
            .catch(err => this.handleError(err));
    }    

    saveObjectLevel(level: any, objectType: string, objectId: number, action: string) {
        level.ID = objectId;
        if (action == 'new') {
            return this.postDynamic(this.http, `${objectType}level`, level);
        }        
        return this.putDynamic(this.http, `${objectType}level`, level);
    }
        
    deleteObjectLevel(objectType: string, objectId: number, levelId: number): Promise<JsonResult> {        
        let headers = new Headers();
        headers.append('Content-Type', 'application/json');

        let options = new RequestOptions({ headers: headers });

        let url = `form/${objectType}/${objectId}/levels/${levelId}`;

        return this.http
            .delete(url, options)
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(err => this.handleError(err));
    }

}