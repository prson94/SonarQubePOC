import { Injectable } from '@angular/core';
import { TooltipInfo, LookupTooltipInfo } from '../models/tooltip-info.model';
import { HttpClient } from '@angular/common/http';
import { Observable, empty } from 'rxjs';
import { catchError, map, publishReplay, refCount } from 'rxjs/operators';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';

@Injectable({
    providedIn: 'root'
})
export class ToolTipService extends BaseObservableService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    getTooltipInfo(objectType: string, objectID: number): Observable<TooltipInfo> {
        if (objectType === undefined || objectID === undefined) return empty(); 

        return this.http.get(`resources/tooltipdata/${objectType}/${objectID}`)
            .pipe(
                map(response => <TooltipInfo>response),
                catchError(err => this.handleError(err))
            );
    }
    private tooltipsCache: any[] = [];

    getTooltipInfoByUid(uid: string, objectType: string = null): Observable<TooltipInfo> {
        var cachedItem = this.tooltipsCache.find(x => x.uid == uid);
        if (cachedItem)
            return cachedItem.obs;

        var obs = this.http.get(`resources/tooltipdatabyuid/${uid}?objectType=${objectType}`)
            .pipe(
                map(response => <TooltipInfo>response),
                publishReplay(1),
                refCount(),
                catchError(err => this.handleError(err))
            );

        var data = { uid: uid, obs: obs };
        this.tooltipsCache.push(data);

        return obs;
    }

    getLookupTooltipInfo(objectType: string, objectID: number): Observable<LookupTooltipInfo> {
        return this.http.get(`resources/lookuptooltipdata/${objectType}/${objectID}`)
            .pipe(
                map(response => <LookupTooltipInfo>response),
                catchError(err => this.handleError(err))
            );
    }
}