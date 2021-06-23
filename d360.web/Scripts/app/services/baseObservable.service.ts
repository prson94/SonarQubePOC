import { Injectable } from '@angular/core';
import { HttpErrorResponse, HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';

import { JsonResult } from '../models/jsonresult.model';

import { MessagesObservableService } from './messages-observable.service';
import { Router } from '@angular/router';

@Injectable({
    providedIn: 'root'
})
export class BaseObservableService {

    constructor(protected messages: MessagesObservableService) {
    }

    handleError(error: HttpErrorResponse, handleAsAPI2Error: boolean = false, router: Router = null) {
        return this.messages.saveClientError(error, handleAsAPI2Error).pipe(
            tap(res => {
                if (error instanceof Error) {
                    // A client-side or network error occurred. Handle it accordingly.
                    console.error('An error occurred[client side]:', error.statusText);
                } else {
                    // server side error
                    console.error('An error occurred[server side]', error);
                    if (error.status !== 0) {

                        if (router && error && error.status) {
                            if (error.status === 404) {
                                router.navigateByUrl('');
                                return;
                            }
                        }

                        let errorMessage = "";
                        const isError_body = Object.keys(error).indexOf("_body") > -1;
                        const isErrorError = Object.keys(error).indexOf("error") > -1;

                        if (isError_body) {
                            errorMessage = JSON.parse(error["_body"]).message;
                        } else {
                            if (isErrorError) {
                                if (error.error !== null) {
                                    errorMessage = error.error.message;
                                }
                                else if (error.message !== null) {
                                    errorMessage = error.message;
                                }
                            } else {
                                errorMessage = error.toString();
                            }
                        }

                        if (errorMessage == null || errorMessage == '') {
                            errorMessage = 'An error has occurred.';
                        }



                        this.messages.showError('Error', errorMessage);
                    }
                }
            })
        )
    }

    protected downloadFile(data: Blob, name: string) {
        let filename = `${name} ${new Date().toDateString()}.xlsx`;
        if (window.navigator.msSaveOrOpenBlob) {
            window.navigator.msSaveOrOpenBlob(data, filename);
        }
        else {
            let url = window.URL.createObjectURL(data);
            let anchor = document.createElement("a");
            anchor.setAttribute("style", "display:none;");
            document.body.appendChild(anchor);
            anchor.setAttribute("download", filename);
            anchor.href = url;
            anchor.click();
        }
    }

    protected deleteDynamicWithResult(
        http: HttpClient,
        type: string,
        id: number
    ): Observable<JsonResult> {
        const httpHeaders = new HttpHeaders(
            {
                'Content-Type': 'application/json'
            }
        );
        const url = `form/dynamicedit/delete/${type}/${id}`;

        return http
            .delete(
                url,
                {
                    headers: httpHeaders
                }
            )
            .pipe(
                map(res => <JsonResult>res),
                catchError(err => this.handleError(err))
            )
            ;
    }

    protected postDynamic(
        http: HttpClient,
        type: string,
        item: any,
        file?: File,
        isCopy?: boolean
    ): Observable<JsonResult> {
        const httpHeaders = new HttpHeaders({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8' //pass as text since its a dynamic object and mvc has issue with dynamic models
        });

        if (file != undefined) {
            let form = new FormData();

            form.append('json', JSON.stringify(item));
            form.append('file', file);

            let method = (isCopy !== undefined) ? 'create' : 'copy';

            return http
                .post(`form/dynamicedit/${method}/${type}`, form)
                .pipe(
                    map(res => <JsonResult>res),
                    catchError(err => this.handleError(err))
                )
                ;
        }

        return http
            .post(
                `form/dynamicedit/create/${type}`,
                'json=' + encodeURIComponent(JSON.stringify(item)),
                {
                    headers: httpHeaders
                }
            )
            .pipe(
                map(res => <JsonResult>res),
                catchError(err => this.handleError(err))
            )
            ;
    }

    protected putDynamic(
        http: HttpClient,
        type: string,
        item: any,
        file?: File
    ): Observable<JsonResult> {
        const httpHeaders = new HttpHeaders({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8' //pass as text since its a dynamic object and mvc has issue with dynamic models
        });

        if (file != undefined) {
            let form = new FormData();

            form.append('json', JSON.stringify(item));
            form.append('file', file);

            return http
                .put(
                    `form/dynamicedit/edit/${type}`,
                    form
                )
                .pipe(
                    map(res => <JsonResult>res),
                    catchError(err => this.handleError(err))
                )
                ;
        }

        return http
            .put(
                `form/dynamicedit/edit/${type}`,
                'json=' + encodeURIComponent(JSON.stringify(item)),
                {
                    headers: httpHeaders
                }
            )
            .pipe(
                map(res => <JsonResult>res),
                catchError(err => this.handleError(err))
            )
            ;
    }

    public isErrorFromFilterExpression(err: any) {
        return err && err.error && err.error.message && err.error.message.indexOf('Invalid filter expression') != -1;
    }
}
