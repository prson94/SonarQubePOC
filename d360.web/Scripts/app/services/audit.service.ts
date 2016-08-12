///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { Audit, AuditResults } from '../models/audit.model';
import { SortOrder } from '../models/enums.model';
import { GridFilterExpression } from '../models/grid-definition.model';

@Injectable()
export class AuditService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getAuditData(objectID: number, objectType: string, pageNum: number, pageSize: number, sortOrder: SortOrder, sortField?: string, filters?: GridFilterExpression[]): Promise<AuditResults> {
        let sortCol = sortField != undefined ? sortField : "";

        let url = `overlays/${objectType}/${objectID}/auditcombined.json?pagenum=${pageNum}&pagesize=${pageSize}&sortdatafield=${sortField}&sortorder=${sortOrder == SortOrder.None ? "" : (sortOrder == SortOrder.Ascending ? "asc" : "desc")}`;
        let indx = 0;

        if (filters != undefined) {
            url += `&filterscount=${filters.length}`;

            for (let filter of filters) {
                url += `&filtervalue${indx}=${filter.value}&filtercondition${indx}=${filter.condition}&filteroperator${indx}=1&filterdatafield${indx}=${filter.field}`;
                indx++;
            }
        }

        return this.http.get(url)
            .toPromise()
            .then(response => <AuditResults>response.json())
            .catch(err => this.handleError(err));
    }

    exportToExcel(objectID: number, objectType: string) {
        window.location.assign(`overlays/${objectType}/${objectID}/download/excel/audit.xls`)
    }
}