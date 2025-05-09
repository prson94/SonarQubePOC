import { Injectable } from '@angular/core';
import { ResponsibilityType, ResponsibilityTypeCount } from '../models/responsibility-type.model';
import { HttpClient } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';

@Injectable({
    providedIn: 'root'
})
export class ResponsibilityTypeService extends BaseObservableService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    getResponsibilityTypes(): Observable<ResponsibilityType[]> {
        return this.http.get('api/ownership/types')
            .pipe(
                map((response) => <ResponsibilityType[]>response),
                catchError((err) => this.handleError(err))
            );
    }

    getAdminResponsibilityTypes(uid?: string): Observable<ResponsibilityType[]> {
        let uidstring = "";
        if (uid)
            {uidstring = `/${uid}`;}
        return this.http.get(`api/v2/responsibilities/types${uidstring}`)
            .pipe(
                map((response) => <ResponsibilityType[]>response),
                catchError((err) => this.handleError(err))
            );
    }

    getResponsibilityTypeBreakdown(): Observable<ResponsibilityTypeCount[]> {
        return this.http.get('queries/ResponsibilityTypeBreakdown')
            .pipe(
                map((response) => <ResponsibilityTypeCount[]>response),
                catchError((err) => this.handleError(err))
            );
    }

    getResponsibilityTypesByObject(type: string, id: number): Observable<any> {
        const uri = `api/ownership/${type}/${id}/responsibilitytypes`;
        return this.http.get(uri)
            .pipe(
                map((response) => <any>response),
                catchError((err) => this.handleError(err))
            );
    }
}