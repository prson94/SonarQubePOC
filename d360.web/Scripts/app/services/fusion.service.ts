import {Injectable} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {Observable} from "rxjs";
import {catchError, map} from "rxjs/operators";

import {JsonResult} from '../models/jsonresult.model';
import {ObjectStyle} from '../models/object-style.model';
import {
    AttributeNode,
    Fusion,
    FusionAgentError,
    FusionAgentExecutionStats,
    FusionAttributeItem,
    FusionAttributeType,
    FusionAttributeTypeCustomQuery,
    FusionConfigurationDetails,
    FusionExecutionError,
    FusionExecutionResultPaged,
    FusionProcessError,
    FusionPromotionExecutionStats,
    FusionQueryAttributeType,
    FusionSchedule,
    FusionSummaryStats,
    FusionType,
    FusionWorkerExecution,
    MapRuleItemDetail,
    RelationIntersectType,
   } from '../models/fusion.model';
import {GridColumn} from '../models/grid-definition.model';
import {SortOrder} from '../models/enums.model';
import {MessagesObservableService} from './messages-observable.service';
import {BaseObservableService} from "./baseObservable.service";
import {FormHelper} from "../models/form.model";
import { TreeNode } from 'primeng/components/common/api';

@Injectable()
export class FusionService extends BaseObservableService {
    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) {
        super(messagesService);
    }

    getFusionTypes(query: string = ''): Observable<FusionType[]> {
        return this
            .http
            .get(`services/fusion?${query}`)
            .pipe(
                map(response => <FusionType[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getFusionAttributeTypes(id: number, query: string = ''): Observable<FusionAttributeType[]> {
        return this
            .http
            .get(`services/fusion/${id}/attributetypes?${query}`)
            .pipe(
                map(response => <FusionAttributeType[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getFusionAttributeTypeTree(id: number, query: string = ''): Observable<TreeNode[]> {
        return this.getFusionAttributeTypes(id, query).pipe(
            map(r => {
                return FormHelper.formTree(r);
            }),
            catchError(err => this.handleError(err))
        );
    }

    getFusionConfiguration(fusionId: number): Observable<FusionConfigurationDetails> {
        return this
            .http
            .get(`services/fusion/configurationById/${fusionId}`)
            .pipe(
                map(response => <FusionConfigurationDetails>response),
                catchError(err => this.handleError(err))
            );
    }

    /* FIXME: looks like never called */
    getFusionConfigurationFromObjectId(fusionAttributeId: number): Observable<FusionConfigurationDetails> {
        return this
            .http
            .get(`services/fusion/configurationByObjectId/${fusionAttributeId}`)
            .pipe(
                map(response => <FusionConfigurationDetails>response),
                catchError(err => this.handleError(err))
            );
    }

    getFusionConfigurations(): Observable<Fusion[]> {
        return this
            .http
            .get(`services/fusion/configurations?$orderby=FusionType,Name`)
            .pipe(
                map(response => <Fusion[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getFusionAgentHistory(
        maxRows?: number,
        fusionId?: number
    ): Observable<FusionAgentExecutionStats[]> {
        var url = `services/fusion/agenthistory?$top=${maxRows ? maxRows : '100'}&$orderby=DateStarted%20desc`;

        if (fusionId) {
            url += `&$filter=FusionID%20eq%20${fusionId}`;
        }

        return this
            .http
            .get(url)
            .pipe(
                map(response => <FusionAgentExecutionStats[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getFusionAgentHistoryExport(
        maxRows?: number,
        fusionId?: number
    ) {
        var url = `services/fusion/agenthistoryexport?top=${maxRows ? maxRows : '100'}`;

        if (fusionId) {
            url += `&fusionId=${fusionId}`;
        }

        this
            .http
            .get(url, {responseType: 'blob'}).subscribe(data => this.downloadFile(data, 'fusion agent history.xlsx'));
    }

    getFusionAgentErrorHistory(
        maxRows?: number,
        days?: number
    ): Observable<FusionAgentError[]> {
        let url = `services/fusion/agenterrors?$top=${maxRows ? maxRows : '100'}&$orderby=Date%20desc`;

        if (days) {
            var d = new Date();

            d.setDate(d.getDate() - days);
            url += `&$filter=Date ge DateTime'${d.toISOString()}'`;
        }

        return this
            .http
            .get(url)
            .pipe(
                map(response => <FusionAgentError[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getFusionAttributeTypeCustomQueries(
        fusionTypeId: number,
        fusionId: number
    ): Observable<FusionAttributeTypeCustomQuery[]> {
        return this
            .http
            .get(`services/fusion/${fusionTypeId}/configurations/${fusionId}/queryoverrides`)
            .pipe(
                map(response => <FusionAttributeTypeCustomQuery[]>response),
                catchError(err => this.handleError(err))
            );
    }

    deleteFusionAttributeTypeCustomQuery(id: number): Observable<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'fusionattributetypecustomquery', id);
    }

    saveFusionAttributeTypeCustomQuery(override: any): Observable<JsonResult> {
        let methodName = "putDynamic";

        if (override.ID == undefined || !override.ID) {
            methodName = "postDynamic";
        }

        return this[methodName](this.http, 'fusionattributetypecustomquery', override);
    }

    getFusionConfigurationSchedules(
        fusionTypeId: number,
        fusionId: number
    ): Observable<FusionSchedule[]> {
        return this
            .http
            .get(`services/fusion/${fusionTypeId}/configurations/${fusionId}/schedules?$orderby=Day,Time`)
            .pipe(
                map(response => <FusionSchedule[]>response),
                catchError(err => this.handleError(err))
            );
    }

    deleteFusionConfiguration(id: number): Observable<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'fusionconfiguration', id);
    }

    deleteFusionConfigurationSchedule(id: number): Observable<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'fusionschedule', id);
    }

    saveFusionConfigurationSchedule(schedule: any): Observable<JsonResult> {
        let methodName = "putDynamic";

        if (schedule.ID == undefined || !schedule.ID) {
            methodName = "postDynamic";
        }

        return this[methodName](this.http, 'fusionschedule', schedule);
    }

    getFusionProcessErrorHistory(
        maxRows?: number,
        days?: number
    ): Observable<FusionProcessError[]> {
        let url = `services/fusion/executionerrors?$top=${maxRows ? maxRows : '100'}&$orderby=Date%20desc`;

        if (days) {
            var d = new Date();

            d.setDate(d.getDate() - days);
            url += `&$filter=Date ge DateTime'${d.toISOString()}'`;
        }

        return this
            .http
            .get(url)
            .pipe(
                map(response => <FusionProcessError[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getFusionWorkerExecutionHistory(
        maxRows?: number,
        fusionId?: number
    ): Observable<FusionWorkerExecution[]> {
        let url = `services/fusion/executionhistory?$top=${maxRows ? maxRows : '100'}&$orderby=DateStarted%20desc`;

        if (fusionId) {
            url += `&$filter=FusionID%20eq%20${fusionId}`;
        }

        return this
            .http
            .get(url)
            .pipe(
                map(response => <FusionWorkerExecution[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getFusionWorkerExecutionHistoryExport(
        maxRows?: number,
        fusionId?: number
    ) {
        let url = `services/fusion/executionhistoryexport?top=${maxRows ? maxRows : '100'}`;

        if (fusionId) {
            url += `&fusionId=${fusionId}`;
        }

        this
            .http
            .get(url, {responseType: 'blob'}).subscribe(data => this.downloadFile(data, 'fusion execution history.xlsx'));
    }

    getFusionStatsSummary(daysToLookBack: number): Observable<FusionSummaryStats> {
        return this
            .http
            .get(`api/fusion/statistics?daysToLookBack=${daysToLookBack}`)
            .pipe(
                map(response => <FusionSummaryStats>response),
                catchError(err => this.handleError(err))
            );
    }

    exportFusionConfigurations() {
        window.location.assign(`services/fusion/configurations/excel.xls`);
    }

    getFusionConfigurationsByType(id: number): Observable<any[]> {
        return this
            .http
            .get(`services/fusion/${id}/configurations?useFieldName=false`)
            .pipe(
                map(response => <any[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getFusionConfigurationGridDefinition(id: number): Observable<GridColumn[]> {
        return this
            .http
            .get(`api/fusiontype/${id}/grid/definition`)
            .pipe(
                map(response => <GridColumn[]>response["Columns"]),
                catchError(err => this.handleError(err))
            );
    }

    getFusionAttributeTypeList(fusionID: number): Observable<FusionAttributeType[]> {
        return this
            .http
            .get(`form/getfusionattributetypes?fusionID=${fusionID}`)
            .pipe(
                map(response => <FusionAttributeType[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getFusionQueryAttributeTypes(
        typeid: number,
        id: number,
        query: string = ''
    ): Observable<FusionQueryAttributeType[]> {
        return this
            .http
            .get(`services/fusion/${typeid}/configurations/${id}/queryattributetypes?${query}`)
            .pipe(
                map(response => <FusionQueryAttributeType[]>response),
                catchError(err => this.handleError(err))
            );
    }

    postFusionType(
        fusionType: FusionType,
        objectStyle: ObjectStyle = null
    ): Observable<any> {
        return this
            .http
            .post('form/FusionType', {fusion: fusionType, style: objectStyle})
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    putFusionType(
        fusionType: FusionType,
        objectStyle: ObjectStyle = null
    ): Observable<any> {
        return this
            .http
            .put('form/FusionType', {fusion: fusionType, style: objectStyle})
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    postFusionAttributeType(
        fusionAttributeType: FusionAttributeType,
        objectStyle: ObjectStyle = null
    ): Observable<any> {
        return this
            .http
            .post('form/FusionAttributeType', {fusion: fusionAttributeType, style: objectStyle})
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    putFusionAttributeType(
        fusionAttributeType: FusionAttributeType,
        objectStyle: ObjectStyle = null
    ): Observable<any> {
        return this
            .http
            .put('form/FusionAttributeType', {fusion: fusionAttributeType, style: objectStyle})
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getFusionTechnicalMappings(): Observable<MapRuleItemDetail[]> {
        return this
            .http
            .get('api/fusion/technicalmapping')
            .pipe(
                map(response => <MapRuleItemDetail[]>response),
                catchError(err => this.handleError(err))
            );
    }


    getFusionFusionAttributeTypes(fusionId: number): Observable<FusionAttributeType[]> {
        return this
            .http
            .get(`services/fusion/${fusionId}/attributetypes?$filter=ScanEnabled eq true&$orderby=Name`)
            .pipe(
                map(response => <FusionAttributeType[]>response),
                catchError(err => this.handleError(err))
            );
    }

   

   

    

    getFusionExecutionErrors(executionId: number): Observable<FusionExecutionError[]> {
        return this
            .http
            .get(`services/fusion/executionerrors?$filter=ExecutionID%20eq%20${executionId}&$orderby=Date%20desc`)
            .pipe(
                map(response => <FusionExecutionError[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getFusionExecutionErrorsExport(executionId: number) {
        this.http.get(`services/fusion/executionerrorsexport/${executionId}`, {responseType: 'blob'}).subscribe(data => this.downloadFile(data, 'fusion execution errors.xlsx'));
    }

    getFusionExecutionResults(
        executionId: number,
        sortField: string,
        sortOrder: SortOrder,
        pageSize: number,
        pageNum: number,
        simpleFilter: string
    ): Observable<FusionExecutionResultPaged> {
        let sortOrderText = sortOrder == SortOrder.None ? "" : (sortOrder == SortOrder.Descending ? "desc" : "asc");
        let url = `services/fusion/executions/${executionId}/results?pagesize=${pageSize}&pagenum=${pageNum}&sortDataField=${sortField}&sortOrder=${sortOrderText}`;

        if (simpleFilter) {
            url += `&filter=${encodeURIComponent(simpleFilter)}`;
        }

        return this
            .http
            .get(url)
            .pipe(
                map(response => <FusionExecutionResultPaged>response),
                catchError(err => this.handleError(err))
            );
    }

    getFusionExecutionResultsExport(executionId: number, simpleFilter: string) {
        let url = `services/fusion/executions/${executionId}/exportresults`;

        if (simpleFilter) {
            url += `?filter=${encodeURIComponent(simpleFilter)}`;
        }

        this.http.get(url, {responseType: 'blob'}).subscribe(data => this.downloadFile(data, 'fusion execution results.xlsx'));
    }

    downloadRawFusionData(executionId: number, name: string) {
        let uri = `internal/fusion/_FusionExecutionRawLog?id=${executionId}`;

        this.http.get(uri, {responseType: 'blob'}).subscribe(data => this.downloadFile(data, name));
    }

    downloadFusionManualLoadTemplate(fusionId: number, fusionTypeId: number, fusionAttributeTypeId: number) {
        let uri = `internal/fusion/${fusionTypeId}/configurations/${fusionId}/template/${fusionAttributeTypeId}`;
        let filename = `Load Template For ${fusionAttributeTypeId}.xlsx`;

        this.http.get(uri, {responseType: 'blob'}).subscribe(data => this.downloadFile(data, filename));
    }

    downloadFile(
        data: Blob,
        filename: string
    ) {
        if (window.navigator.msSaveOrOpenBlob) {
            window.navigator.msSaveOrOpenBlob(data, filename);
        } else {
            var url = window.URL.createObjectURL(data);
            var anchor = document.createElement("a");

            anchor.setAttribute("style", "display:none;");
            document.body.appendChild(anchor);
            anchor.setAttribute("download", filename);
            anchor.href = url;
            anchor.click();
        }
    }

    









    

   

   

   

   

    

    

    
    

    

    

    


    

    getFindAttributeTypes() {
        return this
            .http
            .get('services/fusion/attributetypes')
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }


   

    getFusionConfigurationFromAttributeId(fusionAtttributeId: number): Observable<FusionConfigurationDetails> {
        return this
            .http
            .get(`api/fusion/${fusionAtttributeId}/configurations/fromFusionAttribute`)
            .pipe(
                map(response => <FusionConfigurationDetails>response[0]),
                catchError(err => this.handleError(err))
            );
    }

   

    

    

    deleteFusionQuery(id: number): Observable<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'FusionQueryAttribute', id);
    }

    saveQueryAttributeType(query: FusionQueryAttributeType) {
        let methodName = "putDynamic";

        if (query.ID == undefined || !query.ID) {
            methodName = "postDynamic";
        }

        return this[methodName](this.http, 'fusionqueryattribute', query);
    }

    getPromotionQueryAttributes(ruleID: number): Observable<any> {
        const url = `api/fusion/promotion/QueryAttributes?ruleID=${ruleID}`;
        return this
            .http
            .get(url)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    postRunMarkitLineage(id: number) {
        const url = `form/ScheduleMarkitLineage`;

        return this
            .http
            .post(
                url,
                {id: id}
            ).pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            )
        ;
    }
}
