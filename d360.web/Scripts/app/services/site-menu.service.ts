import { Injectable } from '@angular/core';
import { SiteMenuModel, SiteNav, SiteNavPermission } from '../models/site-menu.model';
import { JsonResult } from '../models/jsonresult.model';
import { HttpClient, HttpContext, HttpHeaders } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';
import { SecondaryNavPostModel } from '../models/secondaryNav.model';
import { IS_QUERY, ROUTE_INDEPENDENT_QUERY } from '../http-interceptors';

@Injectable({
    providedIn: 'root'
})
export class SiteMenuService extends BaseObservableService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    getMenu(): Observable<SiteMenuModel> {
        return this.http
            .get(
                'navigation/sitemenu',
                { context: new HttpContext().set(ROUTE_INDEPENDENT_QUERY, true) }
            )
            .pipe(
                map(response => <SiteMenuModel>response),
                catchError(err => this.handleError(err))
            );
    }

    getAvailableItems(): Observable<SiteNav[]> {
        return this.http.get('navigation/GetAvailableSiteNavigation')
            .pipe(
                map(response => <SiteNav[]>response),
                catchError(err => this.handleError(err))
            );
    }

    addFolderItem(item: SiteNav) {
        return this.http.post('navigation/AddFolderItem', item)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    addFolder(model: any): Observable<JsonResult> {
        return this.http.post('navigation/AddFolder', model)
            .pipe(
                map(response => <JsonResult>response),
                catchError(err => this.handleError(err))
            );
    }

    removeFolderItem(id: number) {
        return this.http.post(`navigation/RemoveFolderItem?id=${id}`, null)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    removeFolder(id: number): Observable<JsonResult> {
        return this.http.post(`navigation/RemoveFolder?id=${id}`, null)
            .pipe(
                map(response => <JsonResult>response),
                catchError(err => this.handleError(err))
            );
    }

    moveFolderUp(id: number) {
        return this.http.put(`navigation/MoveUp?id=${id}`, null)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );

    }

    moveFolderDown(id: number) {
        return this.http.put(`navigation/MoveDown?id=${id}`, null)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    editFolder(folder: SiteNav): Observable<JsonResult> {
        return this.http.put('navigation/EditFolder', folder)
            .pipe(
                map(response => <JsonResult>response),
                catchError(err => this.handleError(err))
            );
    }

    getSiteNavItems(): Observable<SiteNav[]> {
        return this.http
            .get(
                'navigation/GetSiteNavItems',
                { context: new HttpContext().set(ROUTE_INDEPENDENT_QUERY, true) }
            )
            .pipe(
                map(response => <SiteNav[]>response),
                map(r => {
                    r.forEach(s => {
                        s.IsCustom = (s.Name.indexOf('#') != 0);
                    });
                    return r;
                }),
                catchError(err => this.handleError(err))
            );
    }

    getSiteNavFolderItems(folderId: number): Observable<SiteNav[]> {
        return this.http.get(`form/GetSiteNavFolderItems?id=${folderId}`)
            .pipe(
                map(response => <SiteNav[]>response),
                catchError(err => this.handleError(err))
            );
    }

    moveSiteNavFolderUp(id: number, prevID: number) {
        return this.http.put(`navigation/SiteNavFolderMove?targetFolderId=${id}&adjacentFolderId=${prevID}`, null)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    moveSiteNavFolderDown(id: number, nextID: number) {
        return this.http.put(`navigation/SiteNavFolderMove?targetFolderId=${id}&adjacentFolderId=${nextID}`, null)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getSiteNavPermissions(id: number): Observable<SiteNavPermission[]> {
        return this.http.get(`navigation/permissions/get/${id}`)
            .pipe(
                map(response => <SiteNavPermission[]>response),
                catchError(err => this.handleError(err))
            );

    }

    addSiteNavPermission(permission: SiteNavPermission) {
        return this.http.post('navigation/permissions/add', permission)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }


    removeSiteNavPermission(permission: SiteNavPermission) {
        let options = {
            headers: new HttpHeaders({
                'Content-Type': 'application/json'
            }),
            body: {
                permission
            }
        }

        return this.http.delete('navigation/permissions/remove', options)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    setSiteNavPermissions(nav: SiteNav) {
        return this.http.post('navigation/permissions/set', nav)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getSiteNavPermissionsList(id: number = 0): Observable<any[]> {
        return this.http.get(`navigation/permissions/get/list/${id}`)
            .pipe(
                map(response => <any[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getItemCount(url: string) {

        return this.http.get(`navigation/GetItemCount/${url}`)
            .pipe(
                map(response => <number>response),
                catchError(err => this.handleError(err))
            );
    }
    getCounts() {
        return this.http
            .get(
                'navigation/GetCounts',
                { context: new HttpContext().set(ROUTE_INDEPENDENT_QUERY, true) }
            )
            .pipe(
                map(response => <any[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getSecondaryNav(data: SecondaryNavPostModel, preloadTreeData = false) {
        let options = {
            headers: new HttpHeaders({
                'Content-Type': 'application/json'
            }),
            context: new HttpContext().set(IS_QUERY, true)
        }

        return this.http.post(`navigation/secondaryNavigationSettings?preloadData=${preloadTreeData}`, data, options)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }
}