import { Injectable } from '@angular/core';
import {Subject, Observable} from 'rxjs';
import { Breadcrumb } from '../models/breadcrumb.model';
import { HttpClient } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';
import { SiteMenuService } from './site-menu.service';
import { SiteNav } from '../models/site-menu.model';
import { Promise } from 'core-js';
import { resolve } from 'url';


@Injectable()
export class HeaderBreadcrumbService extends BaseObservableService{
    private sitenavservice: SiteMenuService;

    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService,
        sitenavservice: SiteMenuService
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

    // Observable streams
    breadcrumbs$ = this.breadcrumbSource.asObservable();
    breadcrumbClear$ = this.breadcrumbClearSource.asObservable();
    breadcrumbTreeSource$ = this.breadcrumbTreeSource.asObservable();
    breadcrumbPopLastSource$ = this.breadcrumbPopLastSource.asObservable();
    currentObjectInfo$ = this.currentObjectInfoSource.asObservable();

    currentObject: any;


    SiteNavItemsCache: SiteNav[];
    // Service message commands
    
    clearCurrentObjectInfo() {
        this.currentObject = { type: null, id: null };
        this.currentObjectInfoSource.next({ type: null, id: null });
    }

    setCurrentObjectInfo(type: string, id: number) {
        this.currentObject = { type: type, id: id };
        this.currentObjectInfoSource.next({ type: type, id: id });
    }

    showBreadcrumb(breadcrumb: Breadcrumb) {
        this.breadcrumbSource.next(breadcrumb);
    }

    clearBreadcrumbs() {
        this.breadcrumbClearSource.next(true);
    }

    breadcrumbTreeClick(id: number) {
        this.breadcrumbTreeSource.next(id);
    }

    popLastBreadcrumb() {
        this.breadcrumbPopLastSource.next(true);
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

                this.sitenavservice.getSiteNavItems().then(res => {

                    res.forEach(s => {
                        if (s.Name.indexOf(menuID) !== -1) {
                            folderName = s.Title;
                        }
                    });
                }).then(() => {
                    if (folderName != menuID) resolve(folderName);
                    else reject(menuID.substr(1, menuID.length));
                });
            }
        });
        return promise;
    }

    getFolderIcon(menuID: string) {
        let icon = "fa-folder";
        let promise = new Promise<string>((resolve, reject) => {
            if (this.SiteNavItemsCache && this.SiteNavItemsCache.length > 0) {
                this.SiteNavItemsCache.forEach(s => {
                    if (s.Title.indexOf(menuID) !== -1) {
                        icon = s.Icon;
                        if (icon == null && s.FullURL)
                            icon = "URL-" + s.FullURL;
                        else if(icon == null)
                            icon = "fa-folder";
                    }
                });
                if (icon) resolve(icon);

            } else {
                this.sitenavservice.getSiteNavItems().then(res => {

                    res.forEach(s => {
                        if (s.Title.indexOf(menuID) !== -1) {
                            icon = s.Icon;
                            if (icon == null && s.FullURL)
                                icon = "URL-" + s.FullURL;
                            else if(icon == null)
                                icon = "fa-folder";
                        }
                    });
                }).then(() => {
                    if (icon) resolve(icon);
                });
            }
        });
        return promise;
    }
}