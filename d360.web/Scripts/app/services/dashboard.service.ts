import {Injectable} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {Observable} from "rxjs";
import {catchError, map} from "rxjs/operators";

import {Dashboard, DashboardTokens} from '../models/dashboard.model'

import {MessagesObservableService} from './messages-observable.service';
import {BaseObservableService} from "./baseObservable.service";

@Injectable({
    providedIn: 'root'
})
export class DashboardService extends BaseObservableService {

    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) {
        super(messagesService);
    }

    getDashboards(
        objectID: number,
        objectType: string
    ): Observable<Dashboard[]> {
        if (!objectType || objectType == '') objectType = 'Home';
        if (!objectID || objectID == 0) objectID = 0;

        return this
            .http
            .get(`reports/bycontext/${objectType}/${objectID}`)
            .pipe(
                map((response) => <Dashboard[]>response),
                catchError((err) => this.handleError(err))
            );
    }

    getDashboardByID(reportID: number): Observable<Dashboard> {
        return this
            .http
            .get(`reports/byid/${reportID}`)
            .pipe(
                map(response => <Dashboard>response),
                catchError(err => this.handleError(err))
            );
    }

    getPowerBIReportTokens(reportId: string): Observable<DashboardTokens> {
        return this
            .http
            .get(`reports/powerbi/tokens/${reportId}`)
            .pipe(
                map(response => <DashboardTokens>response),
                catchError(err => this.handleError(err))
            );
    }

    getHomePageDashboards(): Observable<Dashboard[]> {
        return this
            .http
            .get('reports/home')
            .pipe(
                map(response => <Dashboard[]>response),
                catchError(err => this.handleError(err))
            );
    }
}
