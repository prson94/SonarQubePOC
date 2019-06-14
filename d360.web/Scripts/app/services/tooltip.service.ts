import { Injectable } from '@angular/core';
import { TooltipInfo, LookupTooltipInfo } from '../models/tooltip-info.model';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';

@Injectable()
export class ToolTipService extends BaseObservableService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    getTooltipInfo(objectType: string, objectID: number): Observable<TooltipInfo> {
        return this.http.get(`resources/tooltipdata/${objectType}/${objectID}`)
            .pipe(
                map(response => <TooltipInfo>response),
                catchError(err => this.handleError(err))
            );
    }

    getLookupTooltipInfo(objectType: string, objectID: number): Observable<LookupTooltipInfo> {
        return this.http.get(`resources/lookuptooltipdata/${objectType}/${objectID}`)
            .pipe(
                map(response => <LookupTooltipInfo>response),
                catchError(err => this.handleError(err))
            );
    }
}