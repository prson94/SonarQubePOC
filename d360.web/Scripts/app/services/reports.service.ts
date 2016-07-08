///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { Report, ReportTile, ReportLayout } from '../models/report.model';
import { JsonResult } from '../models/jsonresult.model';
import { DropdownOption } from '../models/dropdown.model';

@Injectable()
export class ReportsService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getReports(): Promise<Report[]> {
        return this.http.get('reports')
            .toPromise()
            .then(response => <Report[]>response.json())
            .catch(err => this.handleError(err));
    }

    deleteReport(id: number) {
        return this.deleteDynamic(this.http, 'report', id);
    }

    saveReport(report: Report, file?: File): Promise<JsonResult> {
        if (report.ID == undefined || !report.ID) {
            return this.postDynamic(this.http, 'report', report);
        }
        return this.putDynamic(this.http, 'report', report);
    }

    getReportTiles(report: Report): Promise<ReportTile[]> {
        return this.http.get(`reports/${report.ID}/tiles`)
            .toPromise()
            .then(response => <ReportTile[]>response.json())
            .catch(err => this.handleError(err));
    }

    deleteReportTile(id: number) {
        return this.deleteDynamic(this.http, 'reporttile', id);
    }

    saveReportTile(reportTile: ReportTile): Promise<JsonResult> {
        
        if (reportTile.ID == undefined || !reportTile.ID) {
            return this.postDynamic(this.http, 'reporttile', reportTile);
        }
        return this.putDynamic(this.http, 'reporttile', reportTile);
    }


    getReportLayout(report: Report): Promise<ReportLayout> {
        return this.http.get(`reports/${report.ID}/layout`)
            .toPromise()
            .then(response => <ReportLayout>response.json()[0])
            .catch(err => this.handleError(err));
    }

    getReportTargetTypes(): Promise<DropdownOption[]> {        
        return this.http.get('api/reports/targets')
            .toPromise()
            .then(response => <DropdownOption[]>response.json())
            .catch(err => this.handleError(err));
    }

    getReportLayouts(): Promise<DropdownOption[]> {
        return this.http.get('api/reports/layouts')
            .toPromise()
            .then(response => <DropdownOption[]>response.json())
            .catch(err => this.handleError(err));
    }    
}