import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, of } from "rxjs";
import { catchError, map, throwIfEmpty } from "rxjs/operators";

import { BaseObservableService } from "./baseObservable.service";
import { MessagesObservableService } from './messages-observable.service';
import * as _ from 'lodash';

export class CreatedBy {
    uid: string;
    fullName: string;
}

export class UpdatedBy {
    uid: string;
    fullName: string;
}

export class Theme {
    private defaultThemeUid: string = 'AAAAAAAA-0000-0000-0000-000000000001';

    uid: string;
    createdBy: CreatedBy;
    createdOn: Date;
    updatedBy: UpdatedBy;
    updatedOn: Date;
    customCss: string;
    headerLogo: string;
    homeBackground: string;
    icon: string;
    backColor: string;
    breadcrumbLinkColor: string;
    buttonBackColor: string;
    headerBackColor: string;
    isCurrent: boolean;
    name: string;
    navbarBackColor: string;
    navbarBackColorSelected: string;
    primaryButtonBackColor: string;
    tableHeaderBackColor: string;
    tableRowBackColor: string;
    tabLinkColor: string;

    headerLogoUri: string;
    homeBackgroundUri: string;
    iconUri: string;

    svg: string;
    menuItems: any[] = [];

    constructor(setDefaultValues: boolean = false, brandingService: BrandingService = null) {
        if (setDefaultValues) {
            this.headerLogo = brandingService.headerLogoDefault;
            this.icon = brandingService.iconDefault;
            this.homeBackground = brandingService.homeBackgroundDefault;

            this.headerBackColor = "#1E2435";
            this.breadcrumbLinkColor = "#FFFFFF";
            this.buttonBackColor = "#B4B7BE";

            this.navbarBackColor = "#ffffff";
            this.navbarBackColorSelected = "#e4cfff";

            this.primaryButtonBackColor = "#002d4b";
            this.backColor = "#eff0f0";
            this.tabLinkColor = "#002d4b";
            this.tableHeaderBackColor = "#f1f2f3";
            this.tableRowBackColor = "#e4cfff";
        }
    }

    public fillDefaultValues() {
        if (!this.headerBackColor) {
            this.headerBackColor = "#1E2435";
        }
        if (!this.breadcrumbLinkColor) {
            this.breadcrumbLinkColor = "#FFFFFF";
        }
        if (!this.buttonBackColor) {
            this.buttonBackColor = "#B4B7BE";
        }
        if (this.navbarBackColor) {
            this.navbarBackColor = "#ffffff";
        }
        if (!this.navbarBackColorSelected) {
            this.navbarBackColorSelected = "#e4cfff";
        }
        if (!this.primaryButtonBackColor) {
            this.primaryButtonBackColor = "#002d4b";
        }
        if (!this.backColor) {
            this.backColor = "#eff0f0";
        }
        if (!this.tabLinkColor) {
            this.tabLinkColor = "#002d4b";
        }
        if (!this.tableHeaderBackColor) {
            this.tableHeaderBackColor = "#f1f2f3";
        }
        if (!this.tableRowBackColor) {
            this.tableRowBackColor = "#e4cfff";
        }
    }


    public get isDefaultTheme(): boolean {
        return this.uid.toLowerCase() === this.defaultThemeUid.toLowerCase();
    }

    public get hasDownloadOption(): boolean {
        return this.isDefaultTheme ? false : true;
    }

    public get hasEditOption(): boolean {
        return this.isDefaultTheme ? false : true;
    }

    public get hasDuplicateOption(): boolean {
        return true;
    }

    public get hasSetAsCurrentThemeOption(): boolean {
        return this.isCurrent ? false : true;
    }

    public get hasDeleteOption(): boolean {
        return this.isCurrent || this.isDefaultTheme ? false : true;
    }

    public _orig: Theme;
}

@Injectable()
export class BrandingService extends BaseObservableService {
    public headerLogoDefault = "/Content/images/PreciselyLogo@2x.png";
    public iconDefault = "/favicon.ico";
    public homeBackgroundDefault = "/Content/images/HomeBG.png";

    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) {
        super(messagesService);
    }

    public getThemes(): Observable<Theme[] | number> {
        let url: string = '/api/v2/environment/themes';

        return this
            .http
            .get(url)
            .pipe(
                map((res: Theme[]) => {
                    var themes: Theme[] = [];
                    res.forEach((source) => {
                        var target = new Theme();
                        target._orig = source;
                        var sourceProps = Object.keys(source);
                        sourceProps.forEach((prop) => {
                            target[prop] = source[prop];
                        });
                        themes.push(target);
                    });
                    return themes;
                }),
                catchError((err) => {
                    if (err?.status === 409) {
                        return of(0);
                    } else {
                        this.handleError(err, true);
                    }
                })
            );
    }

    private isFromUrl(str: string): boolean {
        return str.toLowerCase().startsWith("http");
    }

    private isDefaultImage(str: string): boolean {
        return this.headerLogoDefault.toLowerCase() === str.toLowerCase()
            || this.homeBackgroundDefault.toLowerCase() === str.toLowerCase()
            || this.iconDefault.toLowerCase() === str.toLowerCase();
    }

    public validateTheme(theme: Theme): Observable<any> {
        let url: string = '/api/v2/environment/themes?validationOnly=true';
        const httpOptions = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' })
        };

        return this
            .http
            .post(url, theme, httpOptions)
            .pipe(
                map((res: any) => {
                    return res;
                }),
                catchError(err => this.handleError(err))
            );
    }

    public saveTheme(theme: Theme): Observable<any> {
        let url: string = '/api/v2/environment/themes';
        const httpOptions = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' })
        };

        //exclude images from request if images are not base64 which means they were not updated
        if (!theme.headerLogo || this.isFromUrl(theme.headerLogo)) {
            delete theme.headerLogo;
        }

        if (!theme.icon || this.isFromUrl(theme.icon)) {
            delete theme.icon;
        }

        if (!theme.homeBackground || this.isFromUrl(theme.homeBackground)) {
            delete theme.homeBackground;
        }

        if (this.isDefaultImage(theme.headerLogo ?? "")) {
            theme.headerLogo = "";
        }

        if (this.isDefaultImage(theme.icon ?? "")) {
            theme.icon = "";
        }

        if (this.isDefaultImage(theme.homeBackground ?? "")) {
            theme.homeBackground = "";
        }

        theme.customCss = theme.customCss ? window.btoa(theme.customCss) : null;


        if (theme.uid) {
            url += "/" + theme.uid;
            return this
                .http
                .put(url, theme, httpOptions)
                .pipe(
                    map((res: any) => {
                        return res;
                    }),
                    catchError(err => this.handleError(err))
                );
        }
        else {
            return this
                .http
                .post(url, theme, httpOptions)
                .pipe(
                    map((res: any) => {
                        return res;
                    }),
                    catchError(err => this.handleError(err))
                );
        }
    }

    public deleteTheme(uid: string): Observable<any> {
        let url: string = '/api/v2/environment/themes/' + uid;
        return this
            .http
            .delete(url)
            .pipe(
                map((res: any) => {
                    return res;
                }),
                catchError(err => this.handleError(err))
            );
    }

    public setAsCurrentTheme(uid: string): Observable<any> {
        let url: string = '/api/v2/environment/themes/' + uid + '/current';
        return this
            .http
            .patch(url, null)
            .pipe(
                map((res: any) => {
                    return res;
                }),
                catchError(err => this.handleError(err))
            );
    }

    public getBase64Data(uid: string): Observable<any> {
        let url: string = '/api/v2/environment/themes/' + uid + '/base64data';
        return this
            .http
            .get(url)
            .pipe(
                map((res: any) => {
                    return res;
                }),
                catchError(err => this.handleError(err))
            );
    }

    public cssToBase64(data: string): Observable<any> {
        const httpOptions = {
            headers: new HttpHeaders({ 'Content-Type': 'text/css' }),
        };
        let url: string = '/api/v2/environment/themes/conversion/base64';
        return this
            .http
            .put(url, data, httpOptions)
            .pipe(
                map((res: any) => {
                    return res;
                }),
                catchError(err => this.handleError(err))
            );
    }
}
