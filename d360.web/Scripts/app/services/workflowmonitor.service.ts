
import {catchError, map} from 'rxjs/operators';
import { Injectable } from "@angular/core";
import { BaseService } from "./base.service";
import { Observable } from "rxjs";
import { Headers, Http, Response, ResponseContentType, RequestOptions } from '@angular/http';
import { MessagesService } from "./messages.service";
import {  WorkflowMonitorItems } from "../models/workflowmonitor.model";
import { GridFilterExpression, GridFilterColumn, GridFilterFieldType } from "../models/grid-definition.model";
import { SortOrder } from "../models/enums.model";




@Injectable()
export class WorkflowMonitorService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

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
        return this.http.get(uri).pipe(
            map(response => {
                return response.json()
            }),
            map(item => { return <WorkflowMonitorItems>item }),
            catchError(err => this.handleError(err)),);
    }

    getWorkFlowMonitorFilterColumnDefinition(): Promise<GridFilterColumn[]> {
        let uri = `services/workflow/workflowmonitor/filter/definition`;
        return this.http.get(uri)
            .toPromise()
            .then(response => <GridFilterColumn[]>response.json())
            .catch(err => this.handleError(err));
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

        this.http.get(uri, { responseType: ResponseContentType.Blob }).subscribe(data => {
            console.log(data);
            this.downloadFile(data, name);
        });
    }

    downloadFile(data: Response, name: string) {
        
        var filename = `${name} Workflow Items ${new Date().toDateString()}.xlsx`;
        if (window.navigator.msSaveOrOpenBlob) {
            window.navigator.msSaveOrOpenBlob(data.blob(), filename);
        }
        else {
            var url = window.URL.createObjectURL(data.blob());
            var anchor = document.createElement("a");
            anchor.setAttribute("style", "display:none;");
            document.body.appendChild(anchor);
            anchor.setAttribute("download", filename);
            anchor.href = url;
            anchor.click();
        }
    }

    deleteItems(itemIds) {
        let options = new RequestOptions( {
            body: itemIds,
        });
        let uri = `services/workflow/deleteItems`;
        return this.http.delete(uri, options)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }
}