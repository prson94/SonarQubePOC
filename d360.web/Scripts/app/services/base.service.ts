import { Injectable } from '@angular/core';
import { Headers, Http, RequestOptions } from '@angular/http';
import { MessagesService } from './messages.service';
import { JsonResult } from '../models/jsonresult.model';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs';


@Injectable()
export class BaseService {
    
    constructor(protected messages: MessagesService) {  }

    handleError(error: HttpErrorResponse) {

        this.messages.saveClientError(error)
            .then(res => {

                if (error instanceof Error) {
                    // A client-side or network error occurred. Handle it accordingly.
                    console.error('An error occurred[client side]:', error.statusText);//error.error.message);
                } else {
                    // server side error
                    console.error('An error occurred[server side]', error);
                    if (this && this.messages && error.status !== 0) {
                        var errorMessage = "";
                        var isError_body = Object.keys(error).indexOf("_body") > -1;

                        if (isError_body) {
                            errorMessage = JSON.parse(error["_body"]).message;
                        } else {
                            errorMessage = error.toString();
                        }
                        
                        this.messages.showError('Error', errorMessage);
                        
                    }
                }
            });

        return Promise.reject(error.error || error);
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
}