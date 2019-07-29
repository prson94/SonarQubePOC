import { Injectable } from '@angular/core';
import { HttpEvent, HttpInterceptor, HttpHandler, HttpRequest, HttpResponse, HttpErrorResponse } from '@angular/common/http';
import { tap, finalize, catchError } from 'rxjs/operators';


import { MessagesObservableService } from '../services/messages-observable.service';

@Injectable()
export class ErrorNotifyInterceptor implements HttpInterceptor {
    m: MessagesObservableService = null;

    constructor(private messenger: MessagesObservableService) {
        this.m = messenger;
    }

    intercept(request: HttpRequest<any>, next: HttpHandler) {
        return next.handle(request).pipe(
            tap(
                event => {
                    status = '';
                    if (event instanceof HttpResponse) {
                        status = 'succeeded';
                    }
                },
                error => {
                    status = 'failed';
                    if (error instanceof HttpErrorResponse) {
                        if (error.error.title && error.error.message) {
                            this.m.showError(error.error.title, error.error.message);
                        }

                        if (error.status === 401 || error.status === 403) {
                            console.log('The authentication session expires or the user is not authorized. Forcing refresh of the current page.');
                            window.location.href = '/slo';
                        }
                    }
                }
            ),
            finalize(() => {

            })
        );

    }
}
