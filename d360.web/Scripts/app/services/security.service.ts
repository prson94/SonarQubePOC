import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';
import { CreateRole, ReadRole } from '../models/security.model';

@Injectable({
    providedIn: 'root'
})
export class SecurityService extends BaseObservableService {
	baseUri: string = "api/v2/security";

	constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    getRoles(): Observable<ReadRole[]> {
		return this.http.get(`${this.baseUri}/roles`)
            .pipe(
				map((response) => <ReadRole[]>response),
                catchError((err) => this.handleError(err))
            );
    }

    updateRole(uid: string, role: ReadRole): Observable<any> {
		return this.http.put(`${this.baseUri}/roles/${uid}`, role)
            .pipe(
                map((response) => response),
                catchError((err) => this.handleError(err))
            );
    }

    createRole(responsibilityType: CreateRole): Observable<ReadRole> {
		return this.http.post(`${this.baseUri}/roles`, responsibilityType)
            .pipe(
                map((response) => response),
                catchError((err) => this.handleError(err))
            );
    }

    deleteRole(uid: string): Observable<any> {
        const httpHeaders = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' })
        };
		return this.http.delete(`${this.baseUri}/roles/${uid}`, httpHeaders)
            .pipe(
                map((response) => response),
                catchError((err) => this.handleError(err))
            );
    }
}