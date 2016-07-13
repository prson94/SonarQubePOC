///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { Audit } from '../models/audit.model';

@Injectable()
export class AuditService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getAuditData(objectID: number, objectType: string): Promise<Audit[]> {
        return this.http.get(`overlays/${objectType}/${objectID}/audit.json?pagenum=0&pagesize=20`)
            .toPromise()
            .then(response => <Audit[]>response.json().results)
            .catch(err => this.handleError(err));
    }
}