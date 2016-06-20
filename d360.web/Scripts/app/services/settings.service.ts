///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { CompanySettings, ICompanySettingsService } from '../models/settings.model';

@Injectable()
export class CompanySettingsService implements ICompanySettingsService {

    constructor(private http: Http) { }

    getSettings(): Promise<CompanySettings> {
        return this.http.get('/form/CompanySettings')
            .toPromise()
            .then(response => <CompanySettings>response.json())
            .catch(this.handleError);
    }

    putSettings(companySettings: CompanySettings): Promise<any> { 
        var headers = new Headers();
        headers.append('Content-Type', 'application/json');

        return this.http.put('/form/UpdateCompanySettings', JSON.stringify(companySettings), { headers: headers })
            .toPromise()
            .catch(this.handleError);
    }

    private handleError(error: any) {
        console.error('An error occurred', error);
        return Promise.reject(error.message || error);
    }
}