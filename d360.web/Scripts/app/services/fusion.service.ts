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
    FusionRule,
    FusionRuleEditorModel,
    FusionRuleFilter,
    FusionRuleFilterEditorModel,
    FusionRuleItem,
    FusionRuleItemEditorModel,
    FusionRuleMapping,
    FusionRuleMappingEditorModel,
    FusionRuleMappingModel,
    FusionRuleStep,
    FusionRuleStepEditorModel,
    FusionSchedule,
    FusionSummaryStats,
    FusionType,
    FusionWorkerExecution,
    MapRuleItemDetail,
    PromotionObject,
    RelationIntersectType,
    RuleStepPromotionHistoryModel
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

    getFusionPromotionHistory(maxRows?: number): Observable<FusionPromotionExecutionStats[]> {
        return this
            .http
            .get(`services/fusion/promotionhistory?$top=${maxRows ? maxRows : '100'}&$orderby=DateStarted%20desc`)
            .pipe(
                map(response => <FusionPromotionExecutionStats[]>response),
                catchError(err => this.handleError(err))
            );
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

    getFusionRules(fusionID: number): Observable<FusionRule[]> {
        return this
            .http
            .get(`api/fusion/${fusionID}/rules`)
            .pipe(
                map(response => <FusionRule[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getFusionRuleSteps(ruleID: number): Observable<FusionRuleStep[]> {
        return this
            .http
            .get(`api/fusion/rules/${ruleID}/steps`)
            .pipe(
                map(response => <FusionRuleStep[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getRuleSteps(
        ruleID: number,
        ruleStepID: number
    ) {
        return this
            .http
            .get(`api/fusion/rule/${ruleID}/steps/${ruleStepID}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getFusionRuleFilters(id: number): Observable<FusionRuleFilter[]> {
        return this
            .http
            .get(`api/fusion/${id}/FusionRuleFilters`)
            .pipe(
                map(response => <FusionRuleFilter[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getFusionRuleItems(id: number): Observable<FusionRuleItem[]> {
        return this
            .http
            .get(`api/fusion/${id}/FusionRuleItems`)
            .pipe(
                map(response => <FusionRuleItem[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getFusionRuleStepMappings(id: number): Observable<FusionRuleMappingModel> {
        return this
            .http
            .get(`api/fusion/${id}/FusionRuleStepMappings`)
            .pipe(
                map(response => <FusionRuleMappingModel>response),
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

    getEditFusionRule(id: number): Observable<FusionRuleEditorModel> {
        return this
            .http
            .get(`form/GetEditFusionRule?id=${id}`)
            .pipe(
                map(response => response),
                map(r => {
                    let m = new FusionRuleEditorModel();

                    m = r["model"];
                    m.AttributeTypes = r["attributeTypes"];

                    return m;
                }),
                catchError(err => this.handleError(err))
            );
    }

    postEditFusionRule(rule: FusionRule): Observable<any> {
        return this
            .http
            .post('form/PostEditFusionRule', rule)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    deleteFusionRuleById(id: number): Observable<any> {
        return this
            .http
            .delete(`form/DeleteFusionRuleById?id=${id}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getAddFusionRule(
        typeID: number,
        fusionID: number
    ): Observable<FusionAttributeItem[]> {
        return this
            .http
            .get(`form/GetAddFusionRule?typeID=${typeID}&fusionID=${fusionID}`)
            .pipe(
                map(response => <FusionAttributeItem[]>response),
                catchError(err => this.handleError(err))
            );
    }

    postAddFusionRule(rule: FusionRule): Observable<any> {
        return this
            .http
            .post('form/PostAddFusionRule', rule)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );

    }

    getAddFusionRuleStep(ruleID: number): Observable<FusionRuleStepEditorModel> {
        return this
            .http
            .get(`form/GetAddFusionRuleStep?ruleID=${ruleID}`)
            .pipe(
                map(response => <FusionRuleStepEditorModel>response),
                catchError(err => this.handleError(err))
            );
    }

    postAddFusionRuleStep(step: FusionRuleStep) {
        return this
            .http
            .post('form/PostAddFusionRuleStep', step)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getEditFusionRuleStep(
        ruleID: number,
        ruleStepID: number
    ): Observable<FusionRuleStepEditorModel> {
        return this
            .http
            .get(`form/GetEditFusionRuleStep?ruleID=${ruleID}&ruleStepID=${ruleStepID}`)
            .pipe(
                map(response => <FusionRuleStepEditorModel>response),
                catchError(err => this.handleError(err))
            );
    }

    putEditFusionRuleStep(step: FusionRuleStep) {
        return this
            .http
            .put('form/PutEditFusionRuleStep', step)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    deleteFusionRuleStep(
        ruleID: number,
        ruleStepID: number
    ) {
        return this
            .http
            .delete(`form/DeleteFusionRuleStepByID?ruleID=${ruleID}&ruleStepID=${ruleStepID}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getAddFusionRuleStepMapping(id: number): Observable<FusionRuleMappingEditorModel> {
        return this
            .http
            .get(`form/GetAddFusionRuleStepMapping?id=${id}`)
            .pipe(
                map(response => <FusionRuleMappingEditorModel>response),
                catchError(err => this.handleError(err))
            );
    }

    postAddFusionRuleStepMapping(mapp: FusionRuleMapping) {
        return this
            .http
            .post('form/PostAddFusionRuleStepMapping', mapp)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    deleteFusionRuleStepMapping(id: number) {
        return this
            .http
            .delete(`form/DeleteFusionRuleStepMappingByID?id=${id}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getEditFusionRuleStepMapping(id: number): Observable<FusionRuleMappingEditorModel> {
        return this
            .http
            .get(`form/GetEditFusionRuleStepMapping?id=${id}`)
            .pipe(
                map(response => <FusionRuleMappingEditorModel>response),
                catchError(err => this.handleError(err))
            );
    }

    putEditFusionRuleStepMapping(mapp: FusionRuleMapping) {
        return this.http.put('form/PutEditFusionRuleStepMapping', mapp)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getAddFusionRuleFilter(id: number): Observable<FusionRuleFilterEditorModel> {
        return this
            .http
            .get(`form/GetAddFusionRuleFilter?id=${id}`)
            .pipe(
                map(response => <FusionRuleFilterEditorModel>response),
                catchError(err => this.handleError(err))
            );
    }

    getEditFusionRuleFilter(id: number): Observable<FusionRuleFilterEditorModel> {
        return this
            .http
            .get(`form/GetEditFusionRuleFilter?id=${id}`)
            .pipe(
                map(response => <FusionRuleFilterEditorModel>response),
                catchError(err => this.handleError(err))
            );
    }

    postAddFusionRuleFilter(form: FusionRuleFilterEditorModel) {
        return this
            .http
            .post('form/AddFusionRuleFilter', form)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    putEditFusionRuleFilter(form: FusionRuleFilterEditorModel) {
        console.log(form);
        return this
            .http
            .put('form/EditFusionRuleFilter', form)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    deleteFusionRuleFilter(id: number) {
        return this
            .http
            .delete(`form/DeleteFusionRuleFilterByID?id=${id}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getFusionRuleFilterTestResults(form: FusionRuleFilterEditorModel) {
        return this
            .http
            .post('form/TestFusionRuleFilter', form)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getAddFusionRuleItem(id: number): Observable<FusionRuleItemEditorModel> {
        return this
            .http
            .get(`form/GetAddFusionRuleItem?id=${id}`)
            .pipe(
                map(response => <FusionRuleItemEditorModel>response),
                catchError(err => this.handleError(err))
            );
    }

    postAddFusionRuleItem(form: any) {
        return this.http.post('form/PostAddFusionRuleItem', form)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    deleteFusionRuleItem(id: number) {
        return this.http.delete(`form/DeleteFusionRuleItemByID?id=${id}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getFusionPromotionItems(
        fusionID: number,
        fusionTypeID: number
    ): Observable<PromotionObject[]> {
        return this
            .http
            .get(`api/fusion/${fusionTypeID}/configurations/${fusionID}/promotion/options`)
            .pipe(
                map(response => <PromotionObject[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getPromotionParents(parentTypeID: number, objectType: string) {
        return this
            .http
            .get(`api/${objectType}/${parentTypeID}/fieldlookup`)
            .pipe(
                map(response => response),
                map(r => {
                    r = r["sort"]((a, b) => {
                        let n1 = (a.Name || '').toUpperCase();
                        let n2 = (b.Name || '').toUpperCase();

                        return (n1 < n2) ? -1 : (n1 > n2) ? 1 : 0;
                    });

                    return r;
                }),
                catchError(err => this.handleError(err))
            );
    }

    getPromotionRuleSteps(
        ruleID: number,
        ruleStepID: number
    ) {
        return this
            .http
            .get(`api/fusion/rule/${ruleID}/steps/${ruleStepID}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getPromotionFusionOwnerRules(fusionID: number) {
        return this
            .http
            .get(`api/fusion/rule/fusionOwners/${fusionID}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getFindSourceFields(
        ruleObjectType: string,
        ruleObjectID: number
    ) {
        return this
            .http
            .get(`fields/${ruleObjectType}/${ruleObjectID}.json`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getFindModels() {
        return this
            .http
            .get('api/catalogs')
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getFindArtifactTypes() {
        return this
            .http
            .get('api/artifacttypes?$orderby=Name')
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getFindFusionAttributeTypes() {
        return this
            .http
            .get('api/fusion/rule/fusionattributetypes')
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
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

    getFindPromotions(fusionAttributeID: number) {
        return this
            .http
            .get(`services/fusion/promotions/${fusionAttributeID}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getFindReferenceItemTypes() {
        return this
            .http
            .get('api/referenceitemtypes?$orderby=Name')
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getLineageRoles() {
        return this
            .http
            .get('/api/fusion/rule/lineage/roles')
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getRelateIntersectTypes() {
        return this
            .http
            .get('/api/fusion/rule/relate/intersectTypes')
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

    getFusionRelationIntersectTypes(): Observable<RelationIntersectType[]> {
        return this
            .http
            .get('/api/fusion/rule/relate/intersectTypes')
            .pipe(
                map(response => <RelationIntersectType[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getPromotionChildAttributeNodes(
        fusionID: number,
        targetFusionAttributeTypeID: number,
        ruleID: number,
        currentFusionAttributeTypeID: number = 0,
        fusionAttributeID: number = 0
    ): Observable<AttributeNode[]> {
        return this
            .http
            .get(`api/fusion/promotion/ChildAttributeNodes?fusionID=${fusionID}&targetFusionAttributeTypeID=${targetFusionAttributeTypeID}&ruleID=${ruleID}&currentFusionAttributeTypeID=${currentFusionAttributeTypeID}&fusionAttributeID=${fusionAttributeID}`)
            .pipe(
                map(response => <AttributeNode[]>response),
                catchError(err => this.handleError(err))
            );
    }

    putMoveFusionRuleStep(
        ruleID: number,
        ruleStepID: number,
        moveUp: boolean
    ) {
        return this
            .http
            .put(`form/MoveFusionRuleStep?ruleID=${ruleID}&ruleStepID=${ruleStepID}&moveUp=${moveUp}`, null)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getFusionRuleStepPromotionHistory(ruleStepID: number): Observable<RuleStepPromotionHistoryModel[]> {
        return this
            .http
            .get(`services/fusion/rules/steps/${ruleStepID}/promotionhistory`)
            .pipe(
                map(response => <RuleStepPromotionHistoryModel[]>response),
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
