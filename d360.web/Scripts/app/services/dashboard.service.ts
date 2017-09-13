import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { Dashboard, DashboardTokens } from '../models/dashboard.model'


@Injectable()
export class DashboardService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getDashboards(objectID: number, objectType: string): Promise<Dashboard[]> {
        if (!objectType || objectType == '') objectType = 'Home';
        if (!objectID || objectID == 0) objectID = 0;
         
        return this.http.get(`reports/bycontext/${objectType}/${objectID}`)
            .toPromise()
            .then(response => <Dashboard[]>response.json())
            .catch(err => this.handleError(err));
    }

    getPowerBIReportTokens(reportId: string): Promise<DashboardTokens> {
        return this.http.get(`reports/powerbi/tokens/${reportId}`)
            .toPromise()
            .then(response => <DashboardTokens>response.json())
            .catch(err => this.handleError(err));
    }

    getHomePageDashboards(): Promise<Dashboard[]> {
        return this.http.get('reports/home')
            .toPromise()
            .then(response => <Dashboard[]>response.json())
            .catch(err => this.handleError(err));
    }
}