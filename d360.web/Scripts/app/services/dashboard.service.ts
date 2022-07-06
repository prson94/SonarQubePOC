import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from "@angular/common/http";
import { Observable } from "rxjs";
import { catchError, map } from "rxjs/operators";

import { DashboardModel, DashboardTokens } from '../models/dashboard.model'

import { MessagesObservableService } from './messages-observable.service';
import { BaseObservableService } from "./baseObservable.service";
import { JsonResult } from '../models/jsonresult.model';
import { DropdownOption } from '../models/dropdown.model';
import { escapeHTML } from 'core-js/fn/string';

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

	getDashboardById(id: number | string): Observable<DashboardModel[]> {
		if (typeof id === 'number') {
			return this.getDashboardsV2(null, null, id);
		}
		return this.getDashboardsV2(id, null, null);
	}

	getDashboardsV2(dashboardUid: string = '', location: number = null, id: number = null, assetTypeUid: string = null, assetUid: string = null): Observable<DashboardModel[]> {
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
		if (assetTypeUid) {
			params["assetTypeUid"] = assetTypeUid;
		}
		if (assetUid) {
			params["assetUid"] = assetUid;
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

	getReportTargetTypes(): Observable<DropdownOption[]> {
		return this.http.get('/api/v2/environment/dashboards/targets')
			.pipe(
				map((response) => <DropdownOption[]>response),
				catchError((err) => this.handleError(err))
			);
	}

	saveDashboard(report: DashboardModel, file?: File): Observable<any> {
		var form = new FormData();
		Object.keys(report).forEach((key) => {
			if (typeof report[key] === 'object') {
				form.append(key, JSON.stringify(report[key]));
			}
			else {
				form.append(key, escapeHTML(report[key]));
			}
		}
		);

		form.append("file", file);

		if (report.uid) {
			return this
				.http
				.put(`api/v2/environment/dashboards`, form)
				.pipe(
					map((res: any) => {
						return res;
					}),
					catchError(err => this.handleError(err))
				);
		}
		else {
			return this
				.http
				.post(`api/v2/environment/dashboards`, form)
				.pipe(
					map((res: any) => {
						return res;
					}),
					catchError(err => this.handleError(err))
				);
		}
	}

	public deleteDashboard(uid: string): Observable<any> {

		const httpOptions = {
			headers: new HttpHeaders({ 'Content-Type': 'application/json' }),
			body: [{ Uid: uid, Cascade: true }]
		};

		return this
			.http
			.delete(`api/v2/environment/dashboards/${uid}`, httpOptions)
			.pipe(
				map(res => <any>res),
				catchError(err => this.handleError(err))
			);
	}


	setPowerBICredentials(user: string, password: string): Observable<JsonResult> {
		var model = { Username: user, Password: password };

		return this
			.http
			.put(`api/v2/environment/dashboards/power-bi-credentials`, model)
			.pipe(
				map((res: any) => {
					return res;
				}),
				catchError(err => this.handleError(err))
			);
	}
}
