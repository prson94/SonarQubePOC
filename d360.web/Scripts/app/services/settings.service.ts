import { Injectable } from "@angular/core";
import { CompanyRebuildJobToken, CompanyRebuildJobStatusApiModel, CompanySettingEnum, SettingsPutModel, SettingsGetModel, AppSettingModel, AppSettingsEnum } from "../models/settings.model";
import { AuthenticationProperties } from "../models/authentication-properties.model";
import { SelectItem } from "primeng/api";
import { JsonResult } from "../models/jsonresult.model";
import { HttpClient, HttpContext, HttpHeaders } from "@angular/common/http";
import { catchError, map, tap } from "rxjs/operators";
import { Observable, of } from "rxjs";
import { BaseObservableService } from "./baseObservable.service";
import { MessagesObservableService } from "./messages-observable.service";
import { OperatorModel } from "../models/operator.model";
import { ROUTE_INDEPENDENT_QUERY } from "../http-interceptors";

@Injectable({
    providedIn: 'root'
})
export class CompanySettingsService extends BaseObservableService {
    appSettings: AppSettingModel[] = null;
    settings: SettingsGetModel[] = null;
    settingToUpdate: SettingsPutModel[] = [];

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    loadApplicationSettings() {
        //HelpBaseUri
        return new Promise((resolve, reject) => {
            this.http
                .get(
                    '/api/v2/environment/appsettings',
                    { context: new HttpContext().set(ROUTE_INDEPENDENT_QUERY, true) }
                ).subscribe((r) => {
                    this.appSettings = <AppSettingModel[]>r;
                    resolve(true);
                });
        });
    }

    loadSettings() {
        return new Promise((resolve, reject) => {
            this.http
                .get(
                    '/api/v2/environment/settings',
                    { context: new HttpContext().set(ROUTE_INDEPENDENT_QUERY, true) }
                ).subscribe((r) => {
                    this.settings = <SettingsGetModel[]>r;
                    // now parse read-only value and load into each model
                    this.settings.forEach((s) => {
                        if (s.BooleanSetting) {
                            s.ScalarValue = s.BooleanSetting.Value;
                        }
                        else if (s.GuidSetting) {
                            s.ScalarValue = s.GuidSetting.Value;
                        }
                        else if (s.NumberSetting) {
                            s.ScalarValue = s.NumberSetting.Value;
                        }
                        else if (s.StringSetting) {
                            s.ScalarValue = s.StringSetting.Value;
                        }
                        else {
                            s.ScalarValue = null;
                        }
                    });
                    resolve(true);
                });
        });
    }

    getSettings(): Observable<SettingsGetModel[]> {
        return of(this.settings);
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

    getAppSetting(setting: string): any {
        return this.appSettings.find((a) => a.Name === setting).Value;
    }

    getSettingById(setting: CompanySettingEnum): SettingsGetModel {
        let settingId: number = <number>setting;
        let foundSetting: SettingsGetModel = null;
        if (this.settings && this.settings.length > 0) {
            foundSetting = this.settings.find((s) => s.SettingID == settingId);
        }
        return foundSetting;
    }

    private parseSettingChange(setting: SettingsPutModel): SettingsPutModel {
        let currentSetting = this.settings.find((s) => s.SettingID === setting.SettingID);

        if (currentSetting.BooleanSetting && setting.BooleanSetting && currentSetting.BooleanSetting.Value !== setting.BooleanSetting.Value) {
            currentSetting.BooleanSetting.Value = setting.BooleanSetting.Value;
            return setting;
        }

        if (currentSetting.GuidSetting && setting.GuidSetting && currentSetting.GuidSetting.Value !== setting.GuidSetting.Value) {
            currentSetting.GuidSetting.Value = setting.GuidSetting.Value;
            return setting;
        }

        if (currentSetting.IpAddressSetting && setting.IpAddressSetting && currentSetting.IpAddressSetting.Value !== setting.IpAddressSetting.Value) {
            currentSetting.IpAddressSetting.Value = setting.IpAddressSetting.Value;
            return setting;
        }

        if (currentSetting.NumberSetting && setting.NumberSetting && currentSetting.NumberSetting.Value !== setting.NumberSetting.Value) {
            currentSetting.NumberSetting.Value = setting.NumberSetting.Value;
            return setting;
        }

        if (currentSetting.StringSetting && setting.StringSetting && currentSetting.StringSetting.Value !== setting.StringSetting.Value) {
            currentSetting.StringSetting.Value = setting.StringSetting.Value;
            return setting;
        }
        return null;
    }

    putSetting(setting: SettingsPutModel): Observable<any> {
        let updatedSetting = this.parseSettingChange(setting);
        var headers = new HttpHeaders({
            'Content-Type': 'application/json'
        });

        return this.http.put('/api/v2/environment/settings', JSON.stringify(updatedSetting), { headers });
    }

    putSettings(settings: SettingsPutModel[]): Observable<any> {

        settings.forEach((s) => {
            var updatedSetting = this.parseSettingChange(s);
            if (updatedSetting !== null) {
                this.settingToUpdate.push(updatedSetting);
            }
        });

        var headers = new HttpHeaders({
            'Content-Type': 'application/json'
        });

        return this.http.put('/api/v2/environment/settings/batch', JSON.stringify(this.settingToUpdate), { headers })
            .pipe(
                tap((_) => {
                    this.settingToUpdate = [];
                    return of({ type: "success" });
                }),
                catchError((err) => {
                    this.handleError(err);
                    return of({ type: "error", title: err.error.title, message: err.error.message });
                })
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