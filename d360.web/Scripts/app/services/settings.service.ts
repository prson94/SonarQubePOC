import { Injectable } from "@angular/core";
import { CompanySettings, ICompanySettingsService, CompanyRebuildJobToken, CompanyRebuildJobStatusApiModel, CompanySettingEnum, SettingsPutModel } from "../models/settings.model";
import { AuthenticationProperties } from "../models/authentication-properties.model";
import { SelectItem } from "primeng/api";
import { JsonResult } from "../models/jsonresult.model";
import { HttpClient, HttpHeaders } from "@angular/common/http";
import { catchError, map } from "rxjs/operators";
import { Observable } from "rxjs";
import { BaseObservableService } from "./baseObservable.service";
import { MessagesObservableService } from "./messages-observable.service";
import { OperatorModel } from "../models/operator.model";

@Injectable({
    providedIn: 'root'
})
export class CompanySettingsService extends BaseObservableService implements ICompanySettingsService {

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
                catchError(err => this.handleError(err))
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

    getRebuildRequestStatuses() {
        return this.http
            .get(`api/v2/environment/rebuilds`)
            .pipe(
                map(res => res as CompanyRebuildJobStatusApiModel[]),
                catchError(err => this.handleError(err))
            );
    }

    postRebuildRequest(jobToken: CompanyRebuildJobToken) {
        return this.http
            .post(`api/v2/environment/rebuilds`, { Job: jobToken })
            .pipe(
                map(res => res as JsonResult),
                catchError(err => this.handleError(err))
            );
    }

    getSettingById(setting: CompanySettingEnum): Observable<any> {
        return this.http
            .get(`/api/v2/environment/settings?_settingId=${setting}`)
            .pipe(
                map(res => <any>res),
                catchError(err => this.handleError(err))
            );
    }

    putSetting(setting: SettingsPutModel): Observable<any> {
        return this.http
            .put(`/api/v2/environment/settings`, setting)
            .pipe(
                map(res => <any>res),
                catchError(err => this.handleError(err))
            );
    }

    public getOperators(isForAdvancedFilters: boolean = false): Observable<OperatorModel[]> {
        let url = "/api/v2/environment/operators";
        if (isForAdvancedFilters) {
            url += "?isForAdvancedFilters=true";
        }
        return this.http
            .get<OperatorModel[]>(url)
            .pipe(catchError(err => this.handleError(err)));
    }

    public getLicensingDetails(): Observable<any> {
        return this.http
            .get<any>("/api/v2/environment/licensing")
            .pipe(catchError(err => this.handleError(err)));
    }
}