import { Injectable } from '@angular/core';
import { Subject, Observable, forkJoin, from } from 'rxjs';
import { Breadcrumb } from '../models/breadcrumb.model';
import { HttpClient } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';
import { SiteMenuService } from './site-menu.service';
import { SiteNav } from '../models/site-menu.model';
import { Promise } from 'core-js';
import { resolve } from 'url';
import { AssetStyleService } from './asset-style.service';
import { AssetTypeStyle } from '../models/asset-type-style.model';
import { Title } from '@angular/platform-browser';
import { Router } from '@angular/router';


@Injectable()
export class HeaderBreadcrumbService extends BaseObservableService {
    private sitenavservice: SiteMenuService;

    constructor(
        private http: HttpClient,
        private router: Router,
        messagesService: MessagesObservableService,
        sitenavservice: SiteMenuService,
        private titleService: Title,
        private assetStyleService: AssetStyleService
    ) {
        super(messagesService);
        this.sitenavservice = sitenavservice;
    }

    // Observable sources
    private breadcrumbSource = new Subject<Breadcrumb>();
    private breadcrumbClearSource = new Subject<boolean>();
    private breadcrumbTreeSource = new Subject<number>();
    private breadcrumbPopLastSource = new Subject<boolean>();
    private currentObjectInfoSource = new Subject<any>();
    private buildFromStorageSource = new Subject<Breadcrumb[]>();
    private currentObjectStateSource = new Subject<string>();
    private updateCurrentObjectPath = new Subject<any>();

    // Observable streams
    breadcrumbs$ = this.breadcrumbSource.asObservable();
    breadcrumbClear$ = this.breadcrumbClearSource.asObservable();
    breadcrumbTreeSource$ = this.breadcrumbTreeSource.asObservable();
    breadcrumbPopLastSource$ = this.breadcrumbPopLastSource.asObservable();
    currentObjectInfo$ = this.currentObjectInfoSource.asObservable();
    buildFromStorage$ = this.buildFromStorageSource.asObservable();
    currentObjectStateSource$ = this.currentObjectStateSource.asObservable();
    updateCurrentObjectPath$ = this.updateCurrentObjectPath.asObservable();
    currentObject: any;


    SiteNavItemsCache: SiteNav[];
    // Service message commands

    reRouteFromBreadcrumbs(url: string) {
        this.router.navigateByUrl(url);
    }

    getCurrentUrl(): string {
        return this.router.url;
    }

    clearCurrentObjectInfo() {
        this.currentObject = { type: null, id: null };
        this.currentObjectInfoSource.next({ type: null, id: null });
    }

    updateCurrentPath(oldValue: string, value: string) {
        this.updateCurrentObjectPath.next({ oldValue, value });
    }

    setCurrentObjectInfo(type: string, id: number) {
        this.currentObject = { type: type, id: id };
        this.currentObjectInfoSource.next({ type: type, id: id });
    }

    showBreadcrumb(breadcrumb: Breadcrumb) {
        this.breadcrumbSource.next(breadcrumb);
    }

    setCurrentObjectState(objectState: string) {
        this.currentObjectStateSource.next(objectState);
    }

    clearBreadcrumbs() {
        this.breadcrumbClearSource.next(true);
        this.currentObjectStateSource.next('');
    }

    breadcrumbTreeClick(id: number) {
        this.breadcrumbTreeSource.next(id);
    }

    popLastBreadcrumb() {
        this.breadcrumbPopLastSource.next(true);
    }
    saveBreacrumbsToStorage(crumbs: Breadcrumb[]) {
        localStorage.setItem("Header_Breadcrumbs", JSON.stringify([...crumbs]));
    }
    getBreadcrumbsFromStorage(): Breadcrumb[] {
        return JSON.parse(localStorage.getItem("Header_Breadcrumbs"));
    }
    buildFromStorage() {
        this.buildFromStorageSource.next(this.getBreadcrumbsFromStorage());
    }

    getAreaName(objectType: string, objectId: number): Observable<string> {

        return this.http
            .get(`api/breadcrumb/getArea?&objectType=${objectType}&objectId=${objectId}`)
            .pipe(
                map(response => <string>response),
                catchError(err => this.handleError(err))
            );
    }

    getFolderTitle(menuID: string) {
        let folderName = menuID;
        let promise = new Promise<string>((resolve, reject) => {
            if (this.SiteNavItemsCache && this.SiteNavItemsCache.length > 0) {
                this.SiteNavItemsCache.forEach(s => {
                    if (s.Name.indexOf(menuID) !== -1) {
                        folderName = s.Title;
                    }
                });

                if (folderName != menuID) resolve(folderName);
                else reject(menuID.substr(1, menuID.length));

            } else {

                this.sitenavservice.getSiteNavItems().subscribe(res => {

                    res.forEach(s => {
                        if (s.Name.indexOf(menuID) !== -1) {
                            folderName = s.Title;
                        }
                    });
                }).add(() => {
                    if (folderName != menuID) resolve(folderName);
                    else reject(menuID.substr(1, menuID.length));
                });
            }
        });
        return promise;
    }

    getAssetFolderIcon(objectType: string, objectID: number, menuID: string): Observable<string> {
        if (!objectID)
            return this.getFolderIcon(menuID);


        var d = forkJoin(this.assetStyleService.getAssetTypeObjectStyle(objectType, objectID), this.getFolderIcon(menuID)).pipe(
            map(([first, second]) => {
                let icon = "fa-folder";
                if (first && first.Icon) {
                    icon = first.Icon;
                } else {
                    icon = second;
                }

                return icon;
            }));

        return d;


    }

    iconFromSiteNav(nav: SiteNav = null): string {
        let retVal = "fa-folder";
        if (nav !== null) {
            if (nav.Icon === null && nav.FullURL) {
                retVal = "URL-" + nav.FullURL;
            } else if (nav.Icon !== null) {
                retVal = nav.Icon;
            }
        }
        return retVal;
    }

    getFolderIcon(menuID: string): Observable<string> {
        let icon = "fa-folder";
        let promise = new Promise<string>((resolve, reject) => {
            if (this.SiteNavItemsCache && this.SiteNavItemsCache.length > 0) {
                const nav = this.SiteNavItemsCache.find((s) => s.Title === menuID);
                icon = this.iconFromSiteNav(nav);
                if (icon) resolve(icon);
            } else {
                this.sitenavservice.getSiteNavItems().subscribe(res => {
                    const nav = res.find((s) => s.Title === menuID);
                    icon = this.iconFromSiteNav(nav);
                }).add(() => {
                    if (icon) resolve(icon);
                });
            }
        });
        return from(promise);
    }

    getTitleService(): Title {
        return this.titleService;
    }
}