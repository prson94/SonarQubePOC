///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { SiteMenu, SiteMenuItem } from '../models/site-menu.model';

@Injectable()
export class SiteMenuService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getMenu(): Promise<SiteMenu[]> {
        return this.http.get('navigation/sitemenu')
            .toPromise()
            .then(response => <SiteMenu[]>response.json())
            .catch(err => this.handleError(err));
    }
}