import { Injectable } from '@angular/core';
import { Headers, Http, RequestOptions } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { SiteMenu, SiteMenuItem, SiteMenuModel, SiteNav, SiteNavPermission } from '../models/site-menu.model';
import { JsonResult } from '../models/jsonresult.model';

@Injectable()
export class SiteMenuService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getMenu(): Promise<SiteMenuModel> {
        return this.http.get('navigation/sitemenu')
            .toPromise()
            .then(response => <SiteMenuModel>response.json())            
            .catch(err => this.handleError(err));
    }

    getAvailableItems(): Promise<SiteNav[]> {
        return this.http.get('navigation/GetAvailableSiteNavigation')
            .toPromise()
            .then(response => <SiteNav[]>response.json())
            .catch(err => this.handleError(err));
    }

    addFolderItem(item: SiteNav) {
        return this.http.post('navigation/AddFolderItem', item)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    addFolder(model: any): Promise<JsonResult> {
        return this.http.post('navigation/AddFolder', model)
            .toPromise()
            .then(response => <JsonResult>response.json())            
            .catch(err => this.handleError(err));
    }

    removeFolderItem(id: number) {
        return this.http.post(`navigation/RemoveFolderItem?id=${id}`, null)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    removeFolder(id: number): Promise<JsonResult> {
        return this.http.post(`navigation/RemoveFolder?id=${id}`, null)
            .toPromise()
            .then(response => <JsonResult>response.json())            
            .catch(err => this.handleError(err));
    }

    moveFolderUp(id: number) {
        return this.http.put(`navigation/MoveUp?id=${id}`, null)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));

    }

    moveFolderDown(id: number) {
        return this.http.put(`navigation/MoveDown?id=${id}`, null)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    editFolder(folder: SiteNav): Promise<JsonResult> {
        return this.http.put('navigation/EditFolder',folder)
            .toPromise()
            .then(response => <JsonResult>response.json())
            .catch(err => this.handleError(err));
    }

    getSiteNavItems(): Promise<SiteNav[]> {
        return this.http.get('navigation/GetSiteNavItems')
            .toPromise()
            .then(response => <SiteNav[]>response.json())
            .then(r => {
                r.forEach(s => {
                    s.IsCustom = (s.Name.indexOf('#') != 0);
                });
                return r;
            })
            .catch(err => this.handleError(err));
    }

    getSiteNavFolderItems(folderId: number): Promise<SiteNav[]> {
        return this.http.get(`form/GetSiteNavFolderItems?id=${folderId}`)
            .toPromise()
            .then(response => <SiteNav[]>response.json())
            .catch(err => this.handleError(err));
    }

    moveSiteNavFolderUp(id: number, prevID: number) {
        return this.http.put(`navigation/SiteNavFolderMove?targetFolderId=${id}&adjacentFolderId=${prevID}`, null)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    moveSiteNavFolderDown(id: number,  nextID: number) {
        return this.http.put(`navigation/SiteNavFolderMove?targetFolderId=${id}&adjacentFolderId=${nextID}`, null)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getSiteNavPermissions(id: number): Promise<SiteNavPermission[]> {
        return this.http.get(`navigation/permissions/get/${id}`)
            .toPromise()
            .then(response => <SiteNavPermission[]>response.json())
            .catch(err => this.handleError(err));

    }

    addSiteNavPermission(permission: SiteNavPermission) {
        return this.http.post('navigation/permissions/add', permission)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }


    removeSiteNavPermission(permission: SiteNavPermission) {
        let options = new RequestOptions({ body: permission });

        return this.http.delete('navigation/permissions/remove', options)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    setSiteNavPermissions(nav: SiteNav) {
        return this.http.post('navigation/permissions/set', nav)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getSiteNavPermissionsList(id: number = 0): Promise<any[]> {
        return this.http.get(`navigation/permissions/get/list/${id}`)
            .toPromise()
            .then(response => <any[]>response.json())
            .catch(err => this.handleError(err));
    }

    getItemCount(url: string) {

        return this.http.get(`navigation/GetItemCount/${url}`)
            .toPromise()
            .then(response => <number>response.json())
            .catch(err => this.handleError(err));
    }
    getCounts() {
        return this.http.get(`navigation/GetCounts`)
            .toPromise()
            .then(response => <any[]>response.json())
            .catch(err => this.handleError(err));
    }
}