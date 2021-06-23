import { switchMap, distinctUntilChanged, debounceTime, map, catchError } from 'rxjs/operators';
import { Injectable } from '@angular/core';
import { JsonResult } from '../models/jsonresult.model';
import { Observable } from 'rxjs';
import { BaseObservableService } from './baseObservable.service';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { MessagesObservableService } from './messages-observable.service';

@Injectable({
    providedIn: 'root'
})
export class UriBasedService extends BaseObservableService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    getItems(uri: string): Observable<any[]> {
        return this.http.get(uri)
            .pipe(
                map(response => <any[]>response),
                catchError(err => this.handleError(err))
            );
    }

    deleteItemWithResult(uri: string, id: number): Observable<JsonResult> {
        let headers = new HttpHeaders();
        headers.append('Content-Type', 'application/json');

        return this.http
            .delete(`${uri}${id}`, { headers })
            .pipe(
                map(res => <JsonResult>res),
                catchError(err => this.handleError(err))
            );
    }

    saveItem(createUri: string, editUri: string, item: any): Observable<JsonResult> {
        if (createUri && (item.ID == undefined || !item.ID)) {
            return this.post(createUri, item);
        }
        return this.put(editUri, item);
    }

    private post(uri: string, item: any): Observable<JsonResult> {
        let headers = new HttpHeaders({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8' //pass as text since its a dynamic object and mvc has issue with dynamic models
        });

        return this.http
            .post(uri, 'json=' + encodeURIComponent(JSON.stringify(item)), { headers: headers })
            .pipe(
                map(res => <JsonResult>res),
                catchError(err => this.handleError(err))
            );
    }

    private put(uri: string, item: any): Observable<JsonResult> {
        let headers = new HttpHeaders({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8' //pass as text since its a dynamic object and mvc has issue with dynamic models
        });

        return this.http
            .put(uri, 'json=' + encodeURIComponent(JSON.stringify(item)), { headers: headers })
            .pipe(
                map(res => <JsonResult>res),
                catchError(err => this.handleError(err))
            );
    }

    getAsObservable(uri: string) {
        return this.http.get(uri).pipe(map(res => <any>res));
    }

    search(uri: string, query: Observable<string>, debounceTimeParametr: number = 300, emptyResults: boolean = false) {
        return query.pipe(debounceTime(debounceTimeParametr),
            distinctUntilChanged(),
            switchMap(query => this.getAsObservable(uri + query)));
    }
}