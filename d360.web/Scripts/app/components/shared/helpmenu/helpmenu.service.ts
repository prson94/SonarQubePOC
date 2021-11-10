import { Injectable } from '@angular/core';
import { JsonResult } from '../../../models/jsonresult.model';
import { HelpMenu } from '../../../models/helpmenu.model';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { BaseObservableService } from '../../../services/baseObservable.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';

@Injectable({
    providedIn: 'root'
})
export class HelpMenuService extends BaseObservableService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    public getHelpMenuItems(): Observable<HelpMenu[]> {
        return this.http.get('api/v2/environment/help')
            .pipe(
                map((response) => <HelpMenu[]>response),
                catchError((err) => this.handleError(err))
            );
    }

    public addHelpMenuItems(addItems: HelpMenu[]): Observable<any[]> {
        var headers = new HttpHeaders({ 'Content-Type': 'application/json' })
        return this.http.post('api/v2/environment/help', JSON.stringify(addItems), { headers })
            .pipe(
                map((response) => <any>response),
                catchError((err) => this.handleError(err))
            );
    }

    public updateHelpMenuItems(updateItems: HelpMenu[]): Observable<any[]> {
        var headers = new HttpHeaders({ 'Content-Type': 'application/json' })
        return this.http.put('api/v2/environment/help', JSON.stringify(updateItems), { headers })
            .pipe(
                map((response) => <any>response),
                catchError((err) => this.handleError(err))
            );
    }

    public deleteHelpMenuItems(deleteItems: HelpMenu[]): Observable<any[]> {
        var model = [];
        deleteItems.forEach(item => {
            model.push({ uid: item.uid })
        })
        const httpHeaders = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' }),
            body: model
        };
        return this.http.delete('api/v2/environment/help', httpHeaders)
            .pipe(
                map((response) => <any>response),
                catchError((err) => this.handleError(err))
            );
    }
}