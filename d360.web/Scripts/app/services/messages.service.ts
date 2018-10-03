import { Injectable } from '@angular/core';
import {Subject} from 'rxjs/Subject';
import {SiteMessage} from '../models/site-message.model';
import { Http } from '@angular/http';
import { HttpErrorResponse } from '@angular/common/http';

@Injectable()
export class MessagesService {    
    // Observable sources
    private errorMessageSource = new Subject<SiteMessage>();
    private infoMessageSource = new Subject<SiteMessage>();
    
    // Observable streams
    errorMessage$ = this.errorMessageSource.asObservable();
    infoMessage$ = this.infoMessageSource.asObservable();

    constructor(private http: Http) {

    }
    // Service message commands
    showError(summary: string, detail: string) {        
        this.errorMessageSource.next(new SiteMessage(summary,detail));
    }

    showInfoMessage(summary: string, detail: string) {
        this.infoMessageSource.next(new SiteMessage(summary, detail));
    }

    saveClientError(error: HttpErrorResponse) {
        let objError: Error
        let model: any;

        if (error instanceof Error) {
            objError = error;
        } else if (error.error instanceof Error) {
            objError = error.error;
        } else {
            objError = new Error(error.toString());
        }

        model = { Name: objError.name, Message: objError.message, Stack: objError.stack };

        return this.http.post('api/log/clienterror', model)
            .toPromise()
            .then(() => Promise.resolve())
            .catch(err => {
                console.log('An error while logging error', err);
            });
    }
}