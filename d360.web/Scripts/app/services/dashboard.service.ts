import { Injectable } from '@angular/core';
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { catchError, map } from "rxjs/operators";

import { Dashboard, DashboardModel, DashboardTokens } from '../models/dashboard.model'

import { MessagesObservableService } from './messages-observable.service';
import { BaseObservableService } from "./baseObservable.service";
import { Param } from '../enums/param.enum';

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
			.get(`reports_no/bycontext/${objectType}/${objectID}`)
			.pipe(
				map((response) => <Dashboard[]>response),
				catchError((err) => this.handleError(err))
			);
	}

	getDashboardByID(reportID: number): Observable<Dashboard> {
		return this
			.http
			.get(`reports_no/byid/${reportID}`)
			.pipe(
				map(response => <Dashboard>response),
				catchError(err => this.handleError(err))
			);
	}

	getDashboardById(id: number | string): Observable<DashboardModel[]> {
		if (typeof id === 'number') {
			return this.getDashboardsV2(null, null, id);
		}
		return this.getDashboardsV2(id, null, null);
	}

	getDashboardsV2(dashboardUid: string = '', location: number = null, id: number = null): Observable<DashboardModel[]> {
		var params = {};
		if (dashboardUid) {
			params["uid"] = dashboardUid;
		}
		if (location) {
			params["location"] = location;
		}
		if (id) {
			params["id"] = id;
		}
		var qString = '';
		if (params) {
			qString = Object.keys(params).map(key => key + '=' + params[key]).join('&');
			if (qString)
				qString = '?' + qString;
		}

		return this
			.http
			.get('/api/v2/environment/dashboards' + qString)
			.pipe(
				map(response => <DashboardModel[]>response),
				catchError(err => this.handleError(err))
			);
	}

	getHomePageDashboards(): Observable<DashboardModel[]> {
		return this.getDashboardsV2('', 3);
	}


	getPowerBIReportTokens(reportId: string): Observable<DashboardTokens> {
		return this
			.http
			.get(`/api/v2/environment/dashboards/${reportId}/powerbi-tokens`)
			.pipe(
				map(response => <DashboardTokens>response),
				catchError(err => this.handleError(err))
			);
	}
}
