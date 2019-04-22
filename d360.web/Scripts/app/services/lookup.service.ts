import {Injectable} from '@angular/core';
import {HttpClient, HttpHeaders} from "@angular/common/http";
import {catchError, map} from "rxjs/operators";
import {Observable} from "rxjs";

import {Lookup} from '../models/lookup.model';
import {JsonResult} from '../models/jsonresult.model';

import {MessagesService} from './messages.service';
import {BaseObservableService} from "./baseObservable.service";

@Injectable()
export class LookupService extends BaseObservableService {
    constructor(
        private http: HttpClient,
        messagesService: MessagesService
    ) {
        super(messagesService);
    }

    getLookups(): Observable<Lookup[]> {
        return this.http.get('resources/_Lookups').pipe(
            map(response => <Lookup[]>response["results"]),
            catchError(err => this.handleError(err))
        );
    }

    deleteLookupType(lookupId: number): Observable<JsonResult> {
        const httpHeaders = new HttpHeaders(
            {
                'Content-Type': 'application/json'
            }
        );
        const url = `form/dynamicedit/delete/LookupType/${lookupId}`;

        return this.http.delete(url, {headers: httpHeaders}).pipe(
            map(res => <JsonResult>res),
            catchError(err => this.handleError(err))
        );
    }

    saveLookup(lookup: Lookup): Observable<JsonResult> {
        if (lookup.ID == undefined || !lookup.ID) {
            return this.post(lookup);
        }

        return this.put(lookup);
    }

    private post(lookup: Lookup): Observable<JsonResult> {
        const httpHeaders = new HttpHeaders(
            {
                'Content-Type': 'application/json'
            }
        );

        return this.http.post("form/AddLookupTypeRaw", JSON.stringify(lookup), {headers: httpHeaders}).pipe(
            map(res => <JsonResult>res),
            catchError(err => this.handleError(err))
        );
    }

    private put(lookup: Lookup): Observable<JsonResult> {
        const httpHeaders = new HttpHeaders(
            {
                'Content-Type': 'application/json'
            }
        );

        return this.http.put('form/EditLookupTypeRaw', JSON.stringify(lookup), {headers: httpHeaders}).pipe(
            map(res => <JsonResult>res),
            catchError(err => this.handleError(err))
        );
    }
}
