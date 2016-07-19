///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { Permission } from '../models/permission.model';

@Injectable()
export class PermissionsService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getPermissions(objectID: number, objectType: string): Promise<Permission> {        
        return this.http.get(`api/${objectType}/${objectID}/permissions`)
            .toPromise()
            .then(response => <Permission>response.json())
            .catch(err => this.handleError(err));
    }
}