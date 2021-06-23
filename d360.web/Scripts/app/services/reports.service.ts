import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Report, ReportTile, ReportLayout } from '../models/report.model';
import { JsonResult } from '../models/jsonresult.model';
import { DropdownOption } from '../models/dropdown.model';
import { Observable } from 'rxjs';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';
import { catchError, map } from 'rxjs/operators';

@Injectable({
    providedIn: 'root'
})
export class ReportsService extends BaseObservableService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    getReports(): Observable<Report[]> {
        return this.http.get('reports/reports')
            .pipe(
                map((response) => <Report[]>response),
                catchError((err) => this.handleError(err))
            );
    }

    deleteReport(id: number): Observable<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'report', id);
    }

    saveReport(report: Report, file?: File): Observable<JsonResult> {
        if (report.VisibleToRoles != null && !report.ShowOnHomePage && report.VisibleToRoles.length > 0) report.VisibleTo = report.VisibleToRoles.join(",");
        else report.VisibleTo = null;
        if (report.ID == undefined || !report.ID) {
            return this.postDynamic(this.http, 'report', report, file, false);
        }
        return this.putDynamic(this.http, 'report', report, file);
    }

    getReportTargetTypes(): Observable<DropdownOption[]> {        
        return this.http.get('api/reports/targets')
            .pipe(
            map((response) => <DropdownOption[]>response),
                catchError((err) =>this.handleError(err))
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
}