import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { JsonResult } from '../models/jsonresult.model';
import { TooltipInfo, LookupTooltipInfo } from '../models/tooltip-info.model';

@Injectable()
export class ToolTipService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getTooltipInfo(objectType: string, objectID: number): Promise<TooltipInfo> {
        return this.http.get(`resources/tooltipdata/${objectType}/${objectID}`)
            .toPromise()
            .then(response => <TooltipInfo>response.json())
            .catch(err => this.handleError(err));
    }

    getLookupTooltipInfo(objectType: string, objectID: number): Promise<LookupTooltipInfo> {
        return this.http.get(`resources/lookuptooltipdata/${objectType}/${objectID}`)
            .toPromise()
            .then(response => <LookupTooltipInfo>response.json())
            .catch(err => this.handleError(err));
    }
}