import { Injectable } from '@angular/core';
import { BaseObservableService } from "./baseObservable.service";
import { MessagesObservableService } from './messages-observable.service';
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { catchError, map } from "rxjs/operators";

@Injectable({
    providedIn: 'root'
})
export class LocaleService extends BaseObservableService {
    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) {
        super(messagesService);
    }

    private observable;    

    public getLocales(): Observable<any[]> {
        this.observable = this.http.get('/content/json/locale.json', {
            observe: 'response'
        }).pipe(
            map((res: any) => {
                this.observable = null;
                return res.body;
            }),
            catchError((err) => this.handleError(err))
        );

        return this.observable;
    }        
}