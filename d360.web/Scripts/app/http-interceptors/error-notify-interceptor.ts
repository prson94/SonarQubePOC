import { Injectable } from '@angular/core';
import { HttpEvent, HttpInterceptor, HttpHandler, HttpRequest, HttpResponse, HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs';




import { tap, finalize, catchError } from 'rxjs/operators';
import { Message } from 'primeng/primeng';
import { MessagesService } from '../services/messages.service';
import { JsonCoreResult } from '../models/jsonresult.model';

@Injectable()
export class ErrorNotifyInterceptor implements HttpInterceptor {

    m: MessagesService = null;

    constructor(private messenger: MessagesService) {
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
                        //if (error.error instanceof JsonCoreResult) {
                        //    this.m.showError(error.error.title, error.error.message);
                        //}
                        if (error.error.title && error.error.message) {
                            this.m.showError(error.error.title, error.error.message);
                        }
                    }
                }
            ),
            finalize(() => {

            })
        );

    }
}