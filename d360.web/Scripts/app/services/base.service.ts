import { Injectable } from '@angular/core';
import { Headers, Http, RequestOptions } from '@angular/http';
import { MessagesService } from './messages.service';
import { JsonResult } from '../models/jsonresult.model';

@Injectable()
export class BaseService {
    
    constructor(protected messages: MessagesService) {  }

    handleError(error: any) {
        console.error('An error occurred', error);
        if (this && this.messages) this.messages.showError('Error', error.toString());
        return Promise.reject(error.message || error);
    }


    protected deleteDynamic(http: Http, type: string, id: number) {
        let headers = new Headers();
        headers.append('Content-Type', 'application/json');

        let url = `form/dynamicedit/delete/${type}/${id}`;

        let options = new RequestOptions({ headers: headers });

        return http
            .delete(url, options)
            .toPromise()
            .catch(err => this.handleError(err));
    }

    protected deleteDynamicWithResult(http: Http, type: string, id: number): Promise<JsonResult> {
        let headers = new Headers();
        headers.append('Content-Type', 'application/json');

        let url = `form/dynamicedit/delete/${type}/${id}`;

        let options = new RequestOptions({ headers: headers });

        return http
            .delete(url, options)
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(err => this.handleError(err));
    }
   
    protected postDynamic(http: Http, type: string, item: any, file?: File, isCopy?: boolean): Promise<JsonResult> {
        
        if (file != undefined) {
            let form = new FormData();

            form.append('json', JSON.stringify(item));
            form.append('file', file);

            let method = ( isCopy !== undefined ) ? 'create': 'copy';

            return http
                .post(`form/dynamicedit/${method}/${type}`, form)
                .toPromise()
                .then(res => <JsonResult>res.json())
                .catch(err => this.handleError(err));
        }
               
        let headers = new Headers({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8' //pass as text since its a dynamic object and mvc has issue with dynamic models
        });

        return http
            .post(`form/dynamicedit/create/${type}`, 'json=' + encodeURIComponent(JSON.stringify(item)), { headers: headers })
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(err => this.handleError(err));
    }

    protected putDynamic(http: Http, type: string, item: any, file?: File): Promise<JsonResult> {        

        if (file != undefined) {
            let form = new FormData();

            form.append('json', JSON.stringify(item));
            form.append('file', file);

            return http
                .put(`form/dynamicedit/edit/${type}`, form)
                .toPromise()
                .then(res => <JsonResult>res.json())
                .catch(err => this.handleError(err));
        }

        let headers = new Headers({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8' //pass as text since its a dynamic object and mvc has issue with dynamic models
        });

        return http
            .put(`form/dynamicedit/edit/${type}`, 'json=' + encodeURIComponent(JSON.stringify(item)), { headers: headers })
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(err => this.handleError(err));
    }

    protected addRequestVerificationHeaders(headers: Headers) {
        headers.append('RequestVerificationToken', (<HTMLInputElement>document.getElementById('antiForgeryToken')).value);
        headers.append('X-Requested-With', 'XMLHttpRequest');
    }
}