import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { CompanySettings, ICompanySettingsService } from '../models/settings.model';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { AuthenticationProperties } from '../models/authentication-properties.model';
import { SelectItem } from 'primeng/primeng';
import { JsonResult } from '../models/jsonresult.model';

@Injectable()
export class CompanySettingsService extends BaseService implements ICompanySettingsService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getSettings(): Promise<CompanySettings> {
        return this.http.get('/form/CompanySettings')
            .toPromise()
            .then(response => <CompanySettings>response.json())
            .catch(err =>this.handleError(err));
    }

    putSettings(companySettings: CompanySettings): Promise<any> { 
        var headers = new Headers();
        headers.append('Content-Type', 'application/json');

        return this.http.put('/form/UpdateCompanySettings', JSON.stringify(companySettings), { headers: headers })
            .toPromise()
            .catch(err => this.handleError(err));
    }    
    
    getAuthenticationModel(): Promise<AuthenticationProperties> {
        return this.http.get('api/authenticationModel')
            .toPromise()
            .then(response => <AuthenticationProperties>response.json())
            .catch(err => this.handleError(err));
    }

    getGroups(): Promise<SelectItem[]> {
        return this.http.get(`/form/CompanySettings/groups`)
            .toPromise()
            .then(response => <SelectItem[]>response.json())
            .catch(err => this.handleError(err));
    }

    postDisplayRebuildRequest(): Promise<JsonResult> {
        return this.http
            .post(`form/rebuildDisplayValues`,'')
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(err => this.handleError(err));
    }

    postIndexRebuildRequest(): Promise<JsonResult> {
        return this.http
            .post(`api/v2/search/rebuildIndex`, '')
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(err => this.handleError(err));
    }
}