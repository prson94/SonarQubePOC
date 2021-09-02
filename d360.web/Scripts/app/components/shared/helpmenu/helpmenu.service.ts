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
        return this.http.get('api/v2/helpmenu')
            .pipe(
                map((response) => <HelpMenu[]>response),
                catchError((err) => this.handleError(err))
            );
    }

    updateHelpMenuItems(addItems: HelpMenu[], deleteItems: HelpMenu[]): Observable<any[]> {
        const httpOptions = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' }),
            body: [{ adds: addItems, deletes: deleteItems }]
        };
        let headers = new HttpHeaders({
            'Content-Type': 'application/json'
        });
        return this.http.post('api/v2/helpmenu', JSON.stringify({ adds: addItems, deletes: deleteItems }), { headers: headers })
            .pipe(
                map((response) => <any>response),
                catchError((err) => this.handleError(err))
            );
    }  
}