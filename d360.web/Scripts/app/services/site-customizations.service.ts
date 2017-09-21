import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { CompanySettings, ICompanySettingsService } from '../models/settings.model';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { AuthenticationProperties } from '../models/authentication-properties.model';
import { JsonResult } from '../models/jsonresult.model';

@Injectable()
export class SiteCustomizationsService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getCustomCss(): Promise<string> {
        return this.http.get('/form/stylecustomizations')
            .toPromise()
            .then(response => <string>response.json())
            .catch(err => this.handleError(err));
    }

    saveCustomCss(css: string): Promise<JsonResult> {
        return this.http.put('form/UpdateStyleCustomizations', { css: css })
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }
}