
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { SiteMenu, SiteMenuItem, SiteMenuModel, SiteNav } from '../models/site-menu.model';

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

    addFolder(model: any) {
        return this.http.post('navigation/AddFolder', model)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    removeFolderItem(id: number) {
        return this.http.post(`navigation/RemoveFolderItem?id=${id}`, null)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    removeFolder(id: number) {
        return this.http.post(`navigation/RemoveFolder?id=${id}`, null)
            .toPromise()
            .then(response => response.json())
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

    renameFolder(id: number, name: string) {
        return this.http.put(`navigation/RenameFolder?id=${id}&name=${name}`, null)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getSiteNavItems(): Promise<SiteNav[]> {
        return this.http.get('navigation/GetSiteNavItems')
            .toPromise()
            .then(response => <SiteNav[]>response.json())
            .then(r => {
                r.forEach(s => {
                    if (s.Name.indexOf('#') == 0) {
                        s.IsCustom = false;
                        s.DisplayName = s.Name.substring(1);
                    } else {
                        s.DisplayName = s.Name;
                        s.IsCustom = true;
                    }
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

    
}