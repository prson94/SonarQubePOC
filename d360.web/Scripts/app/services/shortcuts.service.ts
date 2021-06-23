import { Injectable } from '@angular/core';
import { JsonResult } from '../models/jsonresult.model';
import { Shortcut } from '../models/shortcuts.model';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';

@Injectable({
    providedIn: 'root'
})
export class ShortcutService extends BaseObservableService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }


    public addShortcut(shortcut: Shortcut): Observable<JsonResult> {
        return this.http.post('form/shortcut/add', shortcut)
            .pipe(
                map((response) => <JsonResult>response),
                catchError((err) => this.handleError(err))
            );
    }

    public editShortcut(shortcut: Shortcut): Observable<JsonResult> {
        return this.http.put('form/shortcut/edit', shortcut)
            .pipe(
                map((response) => <JsonResult>response),
                catchError((err) => this.handleError(err))
            );
    }

    public deleteShortcut(id: number): Observable<JsonResult> {
        return this.http.delete(`form/shortcut/delete/${id}`)
            .pipe(
                map((response) => <JsonResult>response),
                catchError((err) => this.handleError(err))
            );
    }

    public getShortcuts(): Observable<Shortcut[]> {
        return this.http.get('form/shortcut/list')
            .pipe(
                map((response) => <Shortcut[]>response),
                catchError((err) => this.handleError(err))
            );
    }
    public moveShortcutUp(id: number): Observable<JsonResult> {
        return this.http.put(`form/shortcut/Move?id=${id}&moveUp=true`, null)
            .pipe(
                map((response) => <JsonResult>response),
                catchError((err) => this.handleError(err))
            );
    }

    public moveShortcutDown(id: number): Observable<JsonResult> {
        return this.http.put(`form/shortcut/Move?id=${id}&moveUp=false`, null)
            .pipe(
                map((response) => <JsonResult>response),
                catchError((err) => this.handleError(err))
            );
    }
  
}