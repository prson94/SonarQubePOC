import { Injectable } from '@angular/core';
import { Observable, of, Subject } from 'rxjs';
import { SiteMessage } from '../models/site-message.model';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { HeaderActionsService } from './header-actions.service';
import { catchError, map } from "rxjs/operators";
import { factories } from 'powerbi-client';

@Injectable()
export class MessagesObservableService {
    // Observable sources
    private errorMessageSource = new Subject<SiteMessage>();
    private infoMessageSource = new Subject<SiteMessage>();

    // Observable streams
    errorMessage$ = this.errorMessageSource.asObservable();
    infoMessage$ = this.infoMessageSource.asObservable();

    constructor(
        private http: HttpClient,
        private headerActionService: HeaderActionsService
    ) {

    }

    // Service message commands
    showError(summary: string, detail: string) {
        this.errorMessageSource.next(new SiteMessage(summary, detail));
    }

    showInfoMessage(summary: string, detail: string) {
        this.infoMessageSource.next(new SiteMessage(summary, detail));
        this.headerActionService.emitCountChange();
    }

    saveClientError(error: HttpErrorResponse, handleAsAPIV2Error: boolean = false): Observable<any> {
        let objError: Error;
        let model: any;


        if (!handleAsAPIV2Error) {
            //Depending on where the error was thrown (http get/post/put method, inside the pipe/map using inbuild httpclient json parser or other runtime error)
            //HttpErrorResponse have slightly different format
            if (error instanceof Error) {
                objError = error;
            } else if (error.error && error.error.error) {
                objError = error.error.error;
            }
            else if (!error.error && error.name === 'HttpErrorResponse') {
                objError = new Error(error.message);
                objError.name = error.name;
            }
            else {
                objError = new Error(error.toString());
            }
            if (error.message)
                objError.message = error.message;
        }
        else {
            objError = new Error(error.error.message);
            objError.name = error.error.title;
        }

        model = { Name: objError.name, Message: objError.message, Stack: objError.stack };

        return this.http.post('api/v2/errors/log/clienterror', model).pipe(
            map(() => {
            }),
            catchError(
                (error) => {
                    console.log('An error while logging error', error);
                    return of(error);
                }
            )
        )
    }
}
