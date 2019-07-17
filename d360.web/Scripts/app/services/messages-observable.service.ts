import {Injectable} from '@angular/core';
import {Observable, of, Subject} from 'rxjs';
import {SiteMessage} from '../models/site-message.model';
import {HttpClient, HttpErrorResponse} from '@angular/common/http';
import {HeaderActionsService} from './header-actions.service';
import {catchError, map} from "rxjs/operators";

@Injectable()
export class MessagesObservableService {
    // Observable sources
    private errorMessageSource = new Subject<SiteMessage>();
    private infoMessageSource = new Subject<SiteMessage>();

    // Observable streams
    errorMessage$ = this.errorMessageSource.asObservable();
    infoMessage$ = this.infoMessageSource.asObservable();
    private timeout: any;

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
        this.emitCountChange();
    }

    private emitCountChange() {
        clearTimeout(this.timeout);
        this.timeout = window.setTimeout(() => { this.headerActionService.emitCountChange(); }, 200);
    }

    saveLegacyClientError(error: Response) {
        let objError: Error;
        let model: any;

        if (error instanceof Error) {
            objError = error;
        } else if (error.body instanceof Error) {
            objError = error.body;
        } else {
            objError = new Error(error.toString());
        }

        model = {Name: objError.name, Message: objError.message, Stack: objError.stack};

        return this.http.post('api/v2/errors/log/clienterror', model).pipe(
            map(() => {}),
            catchError(error => {
                console.log('An error while logging error', error);
                return of(error);
            })
        );
    }

    saveClientError(error: HttpErrorResponse): Observable<any> {
        let objError: Error;
        let model: any;

        if (error instanceof Error) {
            objError = error;
        } else if (error.error instanceof Error) {
            objError = error.error;
        } else {
            objError = new Error(error.toString());
        }

        model = {Name: objError.name, Message: objError.message, Stack: objError.stack};

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
