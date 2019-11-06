import { Injectable } from '@angular/core';
import { BaseObservableService } from "./baseObservable.service";
import { MessagesObservableService } from './messages-observable.service';
import { HttpClient } from "@angular/common/http";
import { Observable, of } from "rxjs";
import { catchError, map } from "rxjs/operators";
import { IconProperties } from '../models/icon-properties.model';

@Injectable()
export class IconService extends BaseObservableService {
    constructor(
            private http: HttpClient,
            messagesService: MessagesObservableService
        )
    {
        super(messagesService);
    }

    data;
    observable;

    public getIconProperties(): Observable<IconProperties[]> {
        if (this.data) {
            return of(this.data);
        } else if (this.observable) {
            return this.observable;
        } else {            
            this.observable = this.http.get('/content/json/fontawesome4x.json', {
                observe: 'response'
            }).pipe(
                map((res: any) => {
                    this.observable = null;
                    this.data = res.body;
                    return this.data;
                }),
                catchError(err => this.handleError(err))
            )
                    
            return this.observable;
        }
    }    
}
