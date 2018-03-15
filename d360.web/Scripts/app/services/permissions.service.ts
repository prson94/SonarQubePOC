import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { Permission } from '../models/permission.model';

@Injectable()
export class PermissionsService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getPermissions(objectID: number, objectType: string): Promise<Permission[]> {        
        return this.http.get(`api/${objectType}/${objectID}/permissions`)
            .toPromise()
            .then(response => <Permission[]>response.json())
            .catch(err => this.handleError(err));
    }

    getPermissionsById(assetID: number): Promise<Permission[]> {
        return this.http.get(`api/${assetID}/permissionsbyid`)
            .toPromise()
            .then(response => <Permission[]>response.json())
            .catch(err => this.handleError(err));
    }
}