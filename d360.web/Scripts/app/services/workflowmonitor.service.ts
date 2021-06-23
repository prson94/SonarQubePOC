
import {catchError, map} from 'rxjs/operators';
import { Injectable } from "@angular/core";
import { BaseObservableService } from './baseObservable.service';
import { Observable } from "rxjs";
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { MessagesObservableService } from './messages-observable.service';
import {  WorkflowMonitorItems } from "../models/workflowmonitor.model";
import { GridFilterExpression, GridFilterColumn, GridFilterFieldType } from "../models/grid-definition.model";
import { SortOrder } from "../models/enums.model";


@Injectable({
    providedIn: 'root'
})
export class WorkflowMonitorService extends BaseObservableService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    getWorkFlowMonitorItems(pagesize: number, pagenum: number, sortfield: string, sortorder: SortOrder, filters?: GridFilterExpression[]): Observable<WorkflowMonitorItems> {
        let uri = `internal/monitor/workflowmonitor/items?pagesize=${pagesize}&pagenum=${pagenum}&sortDataField=${sortfield}&sortorder=${sortorder == SortOrder.None ? "" : (sortorder == SortOrder.Ascending ? "asc" : "desc")}`;

        if (filters != undefined) {

            //#region regular fields

            let normalFilters = filters.filter(f => f.fieldtype == GridFilterFieldType.Normal);
            let count = 0;
            uri += '&filterscount=' + normalFilters.length;

            for (let filter of normalFilters) {
                uri += `&filterdatafield${count}=${filter.field}&filtercondition${count}=${filter.condition}&filtervalue${count}=${encodeURIComponent(filter.value)}`;
                count++;
            }
        }
        return this.http.get(uri)
            .pipe(
                 map(response => <WorkflowMonitorItems> response),
                catchError(err => this.handleError(err))
            );
    }

    getWorkFlowMonitorFilterColumnDefinition(): Observable<GridFilterColumn[]> {
        let uri = `services/workflow/workflowmonitor/filter/definition`;
        return this.http.get(uri)
            .pipe(
                map(response => <GridFilterColumn[]>response),
                catchError(err=>this.handleError(err))
            );
    }

    exportToExcel(pagesize: number, pagenum: number, sortfield: string, sortorder: SortOrder, filters?: GridFilterExpression[]) {
        let uri = `internal/monitor/workflowmonitor/items/download/excel.xls?pagesize=${pagesize}&pagenum=${pagenum}&sortDataField=${sortfield}&sortorder=${sortorder == SortOrder.None ? "" : (sortorder == SortOrder.Ascending ? "asc" : "desc")}`;

        if (filters != undefined) {

            //#region regular fields

            let normalFilters = filters.filter(f => f.fieldtype == GridFilterFieldType.Normal);
            let count = 0;
            uri += '&filterscount=' + normalFilters.length;

            for (let filter of normalFilters) {
                uri += `&filterdatafield${count}=${filter.field}&filtercondition${count}=${filter.condition}&filtervalue${count}=${encodeURIComponent(filter.value)}`;
                count++;
            }
        }

        this.http.get(uri, { responseType: 'blob' }).subscribe(data => {
            console.log(data);
            this.downloadFile(data, name);
        });
    }

    downloadFile(data: Blob, name: string) {
        
        var filename = `${name} Workflow Items ${new Date().toDateString()}.xlsx`;
        if (window.navigator.msSaveOrOpenBlob) {
            window.navigator.msSaveOrOpenBlob(data, filename);
        }
        else {
            var url = window.URL.createObjectURL(data);
            var anchor = document.createElement("a");
            anchor.setAttribute("style", "display:none;");
            document.body.appendChild(anchor);
            anchor.setAttribute("download", filename);
            anchor.href = url;
            anchor.click();
        }
    }

    deleteItems(itemIds):Observable<any> {
        const httpOptions = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' }),
            body: itemIds
        };
        let uri = `services/workflow/deleteItems`;

        return this.http.delete(uri, httpOptions).pipe(
            map(res => res),
            catchError(err => this.handleError(err))
        );
    }
}