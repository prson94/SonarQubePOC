///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { Audit, AuditResults } from '../models/audit.model';
import { SortOrder } from '../models/enums.model';

@Injectable()
export class AuditService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getAuditData(objectID: number, objectType: string, pageNum: number, pageSize: number, sortOrder: SortOrder, sortField?: string): Promise<AuditResults> {
        let sortCol = sortField != undefined ? sortField : "";

        return this.http.get(`overlays/${objectType}/${objectID}/audit.json?pagenum=${pageNum}&pagesize=${pageSize}&sortdatafield=${sortField}&sortorder=${sortOrder == SortOrder.None ? "" : (sortOrder == SortOrder.Ascending ? "asc" : "desc") }`)
            .toPromise()
            .then(response => <AuditResults>response.json())
            .catch(err => this.handleError(err));
    }
}