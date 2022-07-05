import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from "@angular/common/http";
import { Observable } from "rxjs";
import { catchError, map } from "rxjs/operators";

import { Dashboard, DashboardModel, DashboardTokens } from '../models/dashboard.model'

import { MessagesObservableService } from './messages-observable.service';
import { BaseObservableService } from "./baseObservable.service";
import { Report } from '../models/report.model';
import { JsonResult } from '../models/jsonresult.model';
import { DropdownOption } from '../models/dropdown.model';

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

	getReports(): Observable<Report[]> {
		return this.http.get('reports/reports')
			.pipe(
				map((response) => <Report[]>response),
				catchError((err) => this.handleError(err))
			);
	}


	setPowerBICredentials(user: string, password: string): Observable<JsonResult> {
		let headers = new HttpHeaders({
			'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8', //pass as text since its a dynamic object and mvc has issue with dynamic models                        
		});

		return this.http
			.post(`form/AddPowerBICredentials`, `Username=${user}&Password=${password}`, { headers: headers })
			.pipe(
				map((response) => response),
				catchError((err) => this.handleError(err))
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

	getReportTargetTypes(): Observable<DropdownOption[]> {
		return this.http.get('/api/v2/environment/dashboards/targets')
			.pipe(
				map((response) => <DropdownOption[]>response),
				catchError((err) => this.handleError(err))
			);
	}

	saveDashboard(report: DashboardModel, file?: File): Observable<any> {
		var form = new FormData();
		form.append("model", JSON.stringify(report));
		form.append("file", file);

		if (report.uid) {
			return this
				.http
				.put(`api/v2/environment/dashboards/${report.uid}`, form)
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
}
