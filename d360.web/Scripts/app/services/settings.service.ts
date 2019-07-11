import { Injectable } from '@angular/core';
import { CompanySettings, ICompanySettingsService } from '../models/settings.model';
import { AuthenticationProperties } from '../models/authentication-properties.model';
import { SelectItem } from 'primeng/primeng';
import { JsonResult } from '../models/jsonresult.model';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';

@Injectable()
export class CompanySettingsService extends BaseObservableService  implements ICompanySettingsService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    getSettings(): Observable<CompanySettings> {
        return this.http.get('/form/CompanySettings')
            .pipe(
            map(response => <CompanySettings>response),
                catchError(err => this.handleError(err))
                );
    }

    putSettings(companySettings: CompanySettings): Observable<any> {
        var headers = new HttpHeaders({
            'Content-Type': 'application/json'
        });

        return this.http.put('/form/UpdateCompanySettings', JSON.stringify(companySettings), { headers })
            .pipe(
                catchError (err => this.handleError(err))
            );
    }    
    
    getAuthenticationModel(): Observable<AuthenticationProperties> {
        return this.http.get('api/authenticationModel')
            .pipe(
                map(response => <AuthenticationProperties>response),
                catchError(err => this.handleError(err))
            );
    }

    getGroups(): Observable<SelectItem[]> {
        return this.http.get(`/form/CompanySettings/groups`)
            .pipe(
                map(response => <SelectItem[]>response),
                catchError(err => this.handleError(err))
            );
    }

    postDisplayRebuildRequest(): Observable<JsonResult> {
        return this.http
            .post(`form/rebuildDisplayValues`,'')
            .pipe(
                map(res => <JsonResult>res),
                catchError(err => this.handleError(err))
            );
    }

    postIndexRebuildRequest(): Observable<JsonResult> {
        return this.http
            .post(`api/v2/search/rebuildIndex`, '')
            .pipe(
                map(res => <JsonResult>res),
                catchError(err => this.handleError(err))
            );
    }
}