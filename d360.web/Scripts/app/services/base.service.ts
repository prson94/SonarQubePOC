import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import 'rxjs/add/operator/toPromise';
import { MessagesService } from './messages.service';
import { JsonResult } from '../models/jsonresult.model';

@Injectable()
export class BaseService {
    
    constructor(protected messages: MessagesService) {  }

    handleError(error: any) {
        console.error('An error occurred', error);        
        this.messages.showError('Error', error.toString());
        return Promise.reject(error.message || error);
    }


    protected deleteDynamic(http: Http, type: string, id: number) {
        let headers = new Headers();
        headers.append('Content-Type', 'application/json');

        let url = `form/dynamicedit/delete/${type}/${id}`;

        return http
            .delete(url, headers)
            .toPromise()
            .catch(err => this.handleError(err));
    }

    protected postDynamic(http: Http, type: string, item: any): Promise<JsonResult> {
        let headers = new Headers({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8' //pass as text since its a dynamic object and mvc has issue with dynamic models
        });
        return http
            .post(`form/dynamicedit/create/${type}`, 'json=' + JSON.stringify(item), { headers: headers })
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(this.handleError);
    }

    protected putDynamic(http: Http, type: string, item: any): Promise<JsonResult> {
        let headers = new Headers({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8' //pass as text since its a dynamic object and mvc has issue with dynamic models
        });
        return http
            .put(`form/dynamicedit/edit/${type}`, 'json=' + JSON.stringify(item), { headers: headers })
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(this.handleError);
    }
}