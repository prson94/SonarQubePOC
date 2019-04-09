import { Injectable } from '@angular/core';
import { Headers, Http, Response, ResponseContentType  } from '@angular/http';
import { BaseService } from './base.service';
import { MessagesService } from './messages.service';
import { FormHelper } from '../models/form.model';
import { JsonResult } from '../models/jsonresult.model';
import { ObjectStyle } from '../models/object-style.model';
import {
    AttributeNode,
    FusionType,
    FusionAttributeType,
    FusionAttributeTypeCustomQuery,
    FusionAttributeItem,
    FusionQueryAttributeType,
    FusionConfiguration,    
    Fusion,
    FusionSchedule,
    FusionConfigurationDetails,
    FusionAgentExecutionStats,
    FusionWorkerExecution,
    FusionPromotionExecutionStats,
    FusionSummaryStats,
    MapRuleItemDetail,
    FusionRule,
    FusionRuleStep,
    FusionRuleFilter,
    FusionRuleItem,
    FusionRuleMapping,
    FusionAgentError,
    FusionExecutionError,
    FusionExecutionResult,
    FusionProcessError,
    FusionRuleEditorModel,
    FusionRuleStepEditorModel,
    FusionRuleFilterEditorModel,
    FusionRuleItemEditorModel,
    FusionRuleMappingEditorModel,
    PromotionObject,
    RelationIntersectType,
    RuleStepPromotionHistoryModel,
    FusionExecutionResultPaged,
    FusionRuleMappingModel
} from '../models/fusion.model';
import { TreeNode, SelectItem } from 'primeng/components/common/api';
import { GridColumn } from '../models/grid-definition.model';
import { SortOrder } from '../models/enums.model';

@Injectable()
export class FusionService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getFusionTypes(query: string = ''): Promise<FusionType[]> {
        return this.http.get(`services/fusion?${query}`)
            .toPromise()
            .then(response => <FusionType[]>response.json())
            .catch(err => this.handleError(err));
    }

    getFusionAttributeTypes(id: number, query: string = ''): Promise<FusionAttributeType[]> {
        return this.http.get(`services/fusion/${id}/attributetypes?${query}`)
            .toPromise()
            .then(response => <FusionAttributeType[]>response.json())
            .catch(err => this.handleError(err));
    }

    getFusionAttributeTypeTree(id: number, query: string = ''): Promise<TreeNode[]> {
        return this.getFusionAttributeTypes(id, query)
            .then(r => {
                return FormHelper.formTree(r);
            })
            .catch(err => this.handleError(err));
    }

    getFusionConfiguration(fusionId: number): Promise<FusionConfigurationDetails> {
        return this.http.get(`services/fusion/configurationById/${fusionId}`)
            .toPromise()
            .then(response => <FusionConfigurationDetails>response.json())
            .catch(err => this.handleError(err));
    }

    getFusionConfigurationFromObjectId(fusionAttributeId: number): Promise<FusionConfigurationDetails> {
        return this.http.get(`services/fusion/configurationByObjectId/${fusionAttributeId}`)
            .toPromise()
            .then(response => <FusionConfigurationDetails>response.json())
            .catch(err => this.handleError(err));
    }

    getFusionConfigurations(): Promise<Fusion[]> {
        return this.http.get(`services/fusion/configurations?$orderby=FusionType,Name`)
            .toPromise()
            .then(response => <Fusion[]>response.json())
            .catch(err => this.handleError(err));
    }

    getFusionAgentHistory(maxRows?: number, fusionId?: number): Promise<FusionAgentExecutionStats[]> {
        var url = `services/fusion/agenthistory?$top=${maxRows ? maxRows : '100'}&$orderby=DateStarted%20desc`;

        if (fusionId) {
            url += `&$filter=FusionID%20eq%20${fusionId}`;
        }

        return this.http.get(url)
            .toPromise()
            .then(response => <FusionAgentExecutionStats[]>response.json())
            .catch(err => this.handleError(err));
    }

    getFusionAgentHistoryExport(maxRows?: number, fusionId?: number) {
        var url = `services/fusion/agenthistoryexport?top=${maxRows ? maxRows : '100'}`;

        if (fusionId) {
            url += `&fusionId=${fusionId}`;
        }

        this.http.get(url, { responseType: ResponseContentType.Blob }).subscribe(data => this.downloadFile(data, 'fusion agent history.xlsx'));                      
    }
    
    getFusionAgentErrorHistory(maxRows?: number, days?: number): Promise<FusionAgentError[]> {
        let url = `services/fusion/agenterrors?$top=${maxRows ? maxRows : '100'}&$orderby=Date%20desc`;
        if (days) {
            var d = new Date();
            d.setDate(d.getDate() - days);
            url += `&$filter=Date ge DateTime'${d.toISOString()}'`;
        }
        return this.http.get(url)
            .toPromise()
            .then(response => <FusionAgentError[]>response.json())
            .catch(err => this.handleError(err));
    }

    getFusionAttributeTypeCustomQueries(fusionTypeId: number, fusionId: number): Promise<FusionAttributeTypeCustomQuery[]> {
        return this.http.get(`services/fusion/${fusionTypeId}/configurations/${fusionId}/queryoverrides`)
            .toPromise()
            .then(response => <FusionAttributeTypeCustomQuery[]>response.json())
            .catch(err => this.handleError(err));
    }

    deleteFusionAttributeTypeCustomQuery(id: number): Promise<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'fusionattributetypecustomquery', id);
    }

    saveFusionAttributeTypeCustomQuery(override: any): Promise<JsonResult> {
        if (override.ID == undefined || !override.ID) {
            return this.postDynamic(this.http, 'fusionattributetypecustomquery', override);
        }
        return this.putDynamic(this.http, 'fusionattributetypecustomquery', override);
    }

    getFusionConfigurationSchedules(fusionTypeId: number, fusionId: number): Promise<FusionSchedule[]> {     
        return this.http.get(`services/fusion/${fusionTypeId}/configurations/${fusionId}/schedules?$orderby=Day,Time`)
            .toPromise()
            .then(response => <FusionSchedule[]>response.json())
            .catch(err => this.handleError(err));
    }

    deleteFusionConfiguration(id: number): Promise<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'fusionconfiguration', id);
    }

    deleteFusionConfigurationSchedule(id: number): Promise<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'fusionschedule', id);
    }

    saveFusionConfigurationSchedule(schedule: any): Promise<JsonResult> {
        if (schedule.ID == undefined || !schedule.ID) {
            return this.postDynamic(this.http, 'fusionschedule', schedule);
        }
        return this.putDynamic(this.http, 'fusionschedule', schedule);
    }

    getFusionProcessErrorHistory(maxRows?: number, days?: number): Promise<FusionProcessError[]> {
        let url = `services/fusion/executionerrors?$top=${maxRows ? maxRows : '100'}&$orderby=Date%20desc`;
        
        if (days) {
            var d = new Date();
            d.setDate(d.getDate() - days);
            url += `&$filter=Date ge DateTime'${d.toISOString()}'`;
        }
        return this.http.get(url)
            .toPromise()
            .then(response => <FusionProcessError[]>response.json())
            .catch(err => this.handleError(err));
    }

    getFusionWorkerExecutionHistory(maxRows?: number, fusionId?: number): Promise<FusionWorkerExecution[]> {
        let url = `services/fusion/executionhistory?$top=${maxRows ? maxRows : '100'}&$orderby=DateStarted%20desc`;

        if (fusionId) {
            url += `&$filter=FusionID%20eq%20${fusionId}`;
        }

        return this.http.get(url)
            .toPromise()
            .then(response => <FusionWorkerExecution[]>response.json())
            .catch(err => this.handleError(err));
    }

    getFusionWorkerExecutionHistoryExport(maxRows?: number, fusionId?: number) {
        let url = `services/fusion/executionhistoryexport?top=${maxRows ? maxRows : '100'}`;

        if (fusionId) {
            url += `&fusionId=${fusionId}`;
        }

        this.http.get(url, { responseType: ResponseContentType.Blob }).subscribe(data => this.downloadFile(data, 'fusion execution history.xlsx'));                      
    }

    getFusionPromotionHistory(maxRows?: number): Promise<FusionPromotionExecutionStats[]> {
        return this.http.get(`services/fusion/promotionhistory?$top=${maxRows ? maxRows : '100'}&$orderby=DateStarted%20desc`)
            .toPromise()
            .then(response => <FusionPromotionExecutionStats[]>response.json())
            .catch(err => this.handleError(err));
    }

    getFusionStatsSummary(daysToLookBack: number): Promise<FusionSummaryStats> {
        return this.http.get(`api/fusion/statistics?daysToLookBack=${daysToLookBack}`)
            .toPromise()
            .then(response => <FusionSummaryStats>response.json())
            .catch(err => this.handleError(err));
    }

    exportFusionConfigurations() {
        window.location.assign(`services/fusion/configurations/excel.xls`);        
    }

    getFusionConfigurationsByType(id: number): Promise<any[]> {
        return this.http.get(`services/fusion/${id}/configurations?useFieldName=false`)
            .toPromise()
            .then(response => <any[]>response.json())
            .catch(err => this.handleError(err));
    }

    getFusionConfigurationGridDefinition(id: number): Promise<GridColumn[]> {
        return this.http.get(`api/fusiontype/${id}/grid/definition`)
            .toPromise()
            .then(response => <GridColumn[]>response.json().Columns)
            .catch(err => this.handleError(err));
    }
    
    getFusionAttributeTypeList(fusionID: number): Promise<FusionAttributeType[]> {
        return this.http.get(`form/getfusionattributetypes?fusionID=${fusionID}`)
            .toPromise()
            .then(response => <FusionAttributeType[]>response.json())
            .catch(err => this.handleError(err));
    }
    
    getFusionQueryAttributeTypes(typeid: number, id: number, query: string = ''): Promise<FusionQueryAttributeType[]> {
        return this.http.get(`services/fusion/${typeid}/configurations/${id}/queryattributetypes?${query}`)
            .toPromise()
            .then(response => <FusionQueryAttributeType[]>response.json())
            .catch(err => this.handleError(err));
    }
    
    postFusionType(fusionType: FusionType, objectStyle: ObjectStyle = null): Promise<any> {
        return this.http.post('form/FusionType', { fusion: fusionType, style: objectStyle })
            .toPromise().
            then(response => response.json())
            .catch(err => this.handleError(err));
    }

    putFusionType(fusionType: FusionType, objectStyle: ObjectStyle = null): Promise<any> {
        return this.http.put('form/FusionType', { fusion: fusionType, style: objectStyle })
            .toPromise().
            then(response => response.json())
            .catch(err => this.handleError(err));
    }

    postFusionAttributeType(fusionAttributeType: FusionAttributeType, objectStyle: ObjectStyle = null): Promise<any>{
        return this.http.post('form/FusionAttributeType', { fusion: fusionAttributeType, style: objectStyle })
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    putFusionAttributeType(fusionAttributeType: FusionAttributeType, objectStyle: ObjectStyle = null): Promise<any> {
        return this.http.put('form/FusionAttributeType', { fusion: fusionAttributeType, style: objectStyle })
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getFusionTechnicalMappings(): Promise<MapRuleItemDetail[]> {
        return this.http.get('api/fusion/technicalmapping')
            .toPromise()
            .then(response => <MapRuleItemDetail[]>response.json())
            .catch(err => this.handleError(err));
    }


    getFusionFusionAttributeTypes(fusionId: number): Promise<FusionAttributeType[]>{
        return this.http.get(`services/fusion/${fusionId}/attributetypes?$filter=ScanEnabled eq true&$orderby=Name`)
            .toPromise()
            .then(response => <FusionAttributeType[]>response.json())
            .catch(err => this.handleError(err));
    }

    getFusionRules(fusionID: number): Promise<FusionRule[]> {
        return this.http.get(`api/fusion/${fusionID}/rules`)
            .toPromise()
            .then(response => <FusionRule[]>response.json())
            .catch(err => this.handleError(err));
    }

    getFusionRuleSteps(ruleID: number): Promise<FusionRuleStep[]> {
        return this.http.get(`api/fusion/rules/${ruleID}/steps`)
            .toPromise()
            .then(response => <FusionRuleStep[]>response.json())
            .catch(err => this.handleError(err));
    }

    getRuleSteps(ruleID: number, ruleStepID: number) {
        return this.http.get(`api/fusion/rule/${ruleID}/steps/${ruleStepID}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getFusionRuleFilters(id: number): Promise<FusionRuleFilter[]> {
        return this.http.get(`api/fusion/${id}/FusionRuleFilters`)
            .toPromise()
            .then(response => <FusionRuleFilter[]>response.json())
            .catch(err => this.handleError(err));
    }

    getFusionRuleItems(id: number): Promise<FusionRuleItem[]> {
        return this.http.get(`api/fusion/${id}/FusionRuleItems`)
            .toPromise()
            .then(response => <FusionRuleItem[]>response.json())
            .catch(err => this.handleError(err));
    }

    getFusionRuleStepMappings(id: number): Promise<FusionRuleMappingModel> {
                return this.http.get(`api/fusion/${id}/FusionRuleStepMappings`)
            .toPromise()
            .then(response => <FusionRuleMappingModel>response.json())
            .catch(err => this.handleError(err));
    }

    getFusionExecutionErrors(executionId: number): Promise<FusionExecutionError[]> {
        return this.http.get(`services/fusion/executionerrors?$filter=ExecutionID%20eq%20${executionId}&$orderby=Date%20desc`)
            .toPromise()
            .then(response => <FusionExecutionError[]>response.json())
            .catch(err => this.handleError(err));
    }

    getFusionExecutionErrorsExport(executionId: number) {              
        this.http.get(`services/fusion/executionerrorsexport/${executionId}`, { responseType: ResponseContentType.Blob }).subscribe(data => this.downloadFile(data, 'fusion execution errors.xlsx'));
    }

    getFusionExecutionResults(executionId: number, sortField: string, sortOrder: SortOrder, pageSize: number, pageNum: number, simpleFilter: string): Promise<FusionExecutionResultPaged> {
        let sortOrderText = sortOrder == SortOrder.None ? "" : (sortOrder == SortOrder.Descending ? "desc" : "asc");
        let url = `services/fusion/executions/${executionId}/results?pagesize=${pageSize}&pagenum=${pageNum}&sortDataField=${sortField}&sortOrder=${sortOrderText}`;

        if (simpleFilter)
            url += `&filter=${encodeURIComponent(simpleFilter)}`;

        return this.http.get(url)
            .toPromise()
            .then(response => <FusionExecutionResultPaged>response.json())
            .catch(err => this.handleError(err));
    }

    getFusionExecutionResultsExport(executionId: number, simpleFilter: string) { 
        let url = `services/fusion/executions/${executionId}/exportresults`;
        if (simpleFilter)
            url += `?filter=${encodeURIComponent(simpleFilter)}`;  
             
        this.http.get(url, { responseType: ResponseContentType.Blob }).subscribe(data => this.downloadFile(data, 'fusion execution results.xlsx'));                      
    }

    downloadRawFusionData(executionId: number, name:string) {
        let uri = `internal/fusion/_FusionExecutionRawLog?id=${executionId}`;
        this.http.get(uri, { responseType: ResponseContentType.Blob }).subscribe(data => this.downloadFile(data, name));
    }

    downloadFusionManualLoadTemplate(fusionId: number, fusionTypeId: number, fusionAttributeTypeId: number) {
        let uri = `internal/fusion/${fusionTypeId}/configurations/${fusionId}/template/${fusionAttributeTypeId}`;
        let filename = `Load Template For ${fusionAttributeTypeId}.xlsx`;
        this.http.get(uri, { responseType: ResponseContentType.Blob }).subscribe(data => this.downloadFile(data, filename));
    }

    downloadFile(data: Response, filename: string) {        
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
    
    getEditFusionRule(id: number): Promise<FusionRuleEditorModel> {
        return this.http.get(`form/GetEditFusionRule?id=${id}`)
            .toPromise()
            .then(response => response.json())
            .then(r => {
                let m = new FusionRuleEditorModel();
                m = r.model;
                m.AttributeTypes = r.attributeTypes;
                return m;
            })
            .catch(err => this.handleError(err));
    }

    postEditFusionRule(rule: FusionRule): Promise<any> {
        return this.http.post('form/PostEditFusionRule', rule)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    deleteFusionRuleById(id: number): Promise<any> {
        return this.http.delete(`form/DeleteFusionRuleById?id=${id}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getAddFusionRule(typeID: number, fusionID: number): Promise<FusionAttributeItem[]> {
        return this.http.get(`form/GetAddFusionRule?typeID=${typeID}&fusionID=${fusionID}`)
            .toPromise()
            .then(response => <FusionAttributeItem[]>response.json())
            .catch(err => this.handleError(err));
    }

    postAddFusionRule(rule: FusionRule): Promise<any> {
        return this.http.post('form/PostAddFusionRule', rule)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));

    }

    getAddFusionRuleStep(ruleID: number): Promise<FusionRuleStepEditorModel> {
        return this.http.get(`form/GetAddFusionRuleStep?ruleID=${ruleID}`)
            .toPromise()
            .then(response => <FusionRuleStepEditorModel>response.json())
            .catch(err => this.handleError(err));
    }

    postAddFusionRuleStep(step: FusionRuleStep) {
        return this.http.post('form/PostAddFusionRuleStep', step)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getEditFusionRuleStep(ruleID: number, ruleStepID: number): Promise<FusionRuleStepEditorModel> {
        return this.http.get(`form/GetEditFusionRuleStep?ruleID=${ruleID}&ruleStepID=${ruleStepID}`)
            .toPromise()
            .then(response => <FusionRuleStepEditorModel>response.json())
            .catch(err => this.handleError(err));
    }

    putEditFusionRuleStep(step: FusionRuleStep) {
        return this.http.put('form/PutEditFusionRuleStep', step)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    deleteFusionRuleStep(ruleID: number, ruleStepID: number) {
        return this.http.delete(`form/DeleteFusionRuleStepByID?ruleID=${ruleID}&ruleStepID=${ruleStepID}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getAddFusionRuleStepMapping(id: number): Promise<FusionRuleMappingEditorModel> {
        return this.http.get(`form/GetAddFusionRuleStepMapping?id=${id}`)
            .toPromise()
            .then(response => <FusionRuleMappingEditorModel>response.json())
            .catch(err => this.handleError(err));
    }

    postAddFusionRuleStepMapping(map: FusionRuleMapping) {
        return this.http.post('form/PostAddFusionRuleStepMapping', map)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    deleteFusionRuleStepMapping(id: number) {
        return this.http.delete(`form/DeleteFusionRuleStepMappingByID?id=${id}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getEditFusionRuleStepMapping(id: number): Promise<FusionRuleMappingEditorModel> {
        return this.http.get(`form/GetEditFusionRuleStepMapping?id=${id}`)
            .toPromise()
            .then(response => <FusionRuleMappingEditorModel>response.json())
            .catch(err => this.handleError(err));
    }

    putEditFusionRuleStepMapping(map: FusionRuleMapping) {
        return this.http.put('form/PutEditFusionRuleStepMapping', map)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getAddFusionRuleFilter(id: number): Promise<FusionRuleFilterEditorModel> {
        return this.http.get(`form/GetAddFusionRuleFilter?id=${id}`)
            .toPromise()
            .then(response => <FusionRuleFilterEditorModel>response.json())
            .catch(err => this.handleError(err));
    }

    getEditFusionRuleFilter(id: number): Promise<FusionRuleFilterEditorModel> {
        return this.http.get(`form/GetEditFusionRuleFilter?id=${id}`)
            .toPromise()
            .then(response => <FusionRuleFilterEditorModel>response.json())
            .catch(err => this.handleError(err));
    }

    postAddFusionRuleFilter(form: FusionRuleFilterEditorModel) {
        return this.http.post('form/AddFusionRuleFilter', form)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    putEditFusionRuleFilter(form: FusionRuleFilterEditorModel) {
        console.log(form);
        return this.http.put('form/EditFusionRuleFilter', form)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    deleteFusionRuleFilter(id: number) {
        return this.http.delete(`form/DeleteFusionRuleFilterByID?id=${id}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getFusionRuleFilterTestResults(form: FusionRuleFilterEditorModel) {
        return this.http.post('form/TestFusionRuleFilter', form)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getAddFusionRuleItem(id: number): Promise<FusionRuleItemEditorModel> {
        return this.http.get(`form/GetAddFusionRuleItem?id=${id}`)
            .toPromise()
            .then(response => <FusionRuleItemEditorModel>response.json())
            .catch(err => this.handleError(err));
    }

    postAddFusionRuleItem(form: any) {
        return this.http.post('form/PostAddFusionRuleItem', form)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    deleteFusionRuleItem(id: number) {
        return this.http.delete(`form/DeleteFusionRuleItemByID?id=${id}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getFusionPromotionItems(fusionID: number, fusionTypeID: number): Promise<PromotionObject[]> {
        return this.http.get(`api/fusion/${fusionTypeID}/configurations/${fusionID}/promotion/options`)
            .toPromise()
            .then(response => <PromotionObject[]>response.json())
            .catch(err => this.handleError(err));
    }

    getPromotionParents(parentTypeID: number, objectType: string ) {
        return this.http.get(`api/${objectType}/${parentTypeID}/fieldlookup`)
            .toPromise()
            .then(response => response.json())
            .then(r => {
                r = r.sort((a, b) => {
                    let n1 = (a.Name || '').toUpperCase();
                    let n2 = (b.Name || '').toUpperCase();

                    return (n1 < n2) ? -1 : (n1 > n2) ? 1 : 0;
                });
                return r;
            })
            .catch(err => this.handleError(err));
    }

    getPromotionRuleSteps(ruleID: number, ruleStepID: number) {
        return this.http.get(`api/fusion/rule/${ruleID}/steps/${ruleStepID}`)
            .toPromise() 
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getPromotionFusionOwnerRules(fusionID: number) {
        return this.http.get(`api/fusion/rule/fusionOwners/${fusionID}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getFindSourceFields(ruleObjectType: string, ruleObjectID: number) {
        return this.http.get(`fields/${ruleObjectType}/${ruleObjectID}.json`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getFindModels() {
        return this.http.get('api/catalogs')
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getFindArtifactTypes() {
        return this.http.get('api/artifacttypes?$orderby=Name')
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getFindFusionAttributeTypes() {
        return this.http.get('api/fusion/rule/fusionattributetypes')
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getFindAttributeTypes() {
        return this.http.get('services/fusion/attributetypes')
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getFindPromotions(fusionAttributeID: number) {
        return this.http.get(`services/fusion/promotions/${fusionAttributeID}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getFindReferenceItemTypes() {
        return this.http.get('api/referenceitemtypes?$orderby=Name')
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getLineageRoles() {
        return this.http.get('/api/fusion/rule/lineage/roles')
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getRelateIntersectTypes() {
        return this.http.get('/api/fusion/rule/relate/intersectTypes')
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getFusionConfigurationFromAttributeId(fusionAtttributeId: number): Promise<FusionConfigurationDetails> {
        return this.http.get(`api/fusion/${fusionAtttributeId}/configurations/fromFusionAttribute`)
            .toPromise()
            .then(response => <FusionConfigurationDetails>response.json()[0])
            .catch(err => this.handleError(err));
    }

    getFusionRelationIntersectTypes(): Promise<RelationIntersectType[]> {
        return this.http.get('/api/fusion/rule/relate/intersectTypes')
            .toPromise()
            .then(response => <RelationIntersectType[]>response.json())
            .catch(err => this.handleError(err));
    }

    getPromotionChildAttributeNodes(fusionID: number,
        targetFusionAttributeTypeID: number,
        ruleID: number,
        currentFusionAttributeTypeID: number = 0,
        fusionAttributeID: number = 0): Promise<AttributeNode[]> {
        return this.http.get(`api/fusion/promotion/ChildAttributeNodes?fusionID=${fusionID}&targetFusionAttributeTypeID=${targetFusionAttributeTypeID}&ruleID=${ruleID}&currentFusionAttributeTypeID=${currentFusionAttributeTypeID}&fusionAttributeID=${fusionAttributeID}`)
            .toPromise()
            .then(response => <AttributeNode[]>response.json())
            .catch(err => this.handleError(err));
    }    

    putMoveFusionRuleStep(ruleID: number, ruleStepID: number, moveUp: boolean) {

        return this.http.put(`form/MoveFusionRuleStep?ruleID=${ruleID}&ruleStepID=${ruleStepID}&moveUp=${moveUp}`, null)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getFusionRuleStepPromotionHistory(ruleStepID: number): Promise<RuleStepPromotionHistoryModel[]> {
        return this.http.get(`services/fusion/rules/steps/${ruleStepID}/promotionhistory`)
            .toPromise()
            .then(response => <RuleStepPromotionHistoryModel[]>response.json())
            .catch(err => this.handleError(err));
    }

    deleteFusionQuery(id: number): Promise<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'FusionQueryAttribute', id);
    }

    saveQueryAttributeType(query: FusionQueryAttributeType) {
        if (query.ID == undefined || !query.ID) {
            return this.postDynamic(this.http, 'fusionqueryattribute', query);
        }
        return this.putDynamic(this.http, 'fusionqueryattribute', query);
    }

    getPromotionQueryAttributes(ruleID: number): Promise<any[]> {
        return this.http.get(`api/fusion/promotion/QueryAttributes?ruleID=${ruleID}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    postRunMarkitLineage(id: number) {
        return this.http.post(`form/ScheduleMarkitLineage`, { id: id })
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }
}