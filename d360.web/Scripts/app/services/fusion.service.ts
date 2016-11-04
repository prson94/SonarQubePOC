import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { BaseService } from './base.service';
import { MessagesService } from './messages.service';
import { FormHelper } from '../models/form.model';
import { JsonResult } from '../models/jsonresult.model';
import { ObjectStyle } from '../models/object-style.model';
import {
    AttributeNode,
    FusionType,
    FusionAttributeType,
    FusionQueryAttributeType,
    FusionConfiguration,
    FusionFilter,    
    Fusion,
    FusionConfigurationDetails,
    FusionAgentExecutionStats,
    FusionWorkerExecution,
    FusionPromotionExecutionStats,
    FusionSummaryStats,
    MapRuleItemDetail,
    FusionRule,
    FusionRuleStep,
    FusionRuleItem,
    FusionRuleMapping,
    FusionAgentError,
    FusionExecutionError,
    FusionExecutionResult,
    FusionProcessError,
    FusionRuleEditorModel,
    FusionRuleStepEditorModel,
    FusionRuleItemEditorModel,
    FusionRuleMappingEditorModel,
    PromotionObject,
    RelationIntersectType,
} from '../models/fusion.model';
import { TreeNode, SelectItem } from 'primeng/primeng';
import { GridColumn } from '../models/grid-definition.model';

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
        return this.http.get(`services/fusion/${id}/configurations`)
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

    getFusionConfigurationFilters(fusionTypeID: number, fusionID: number): Promise<FusionFilter[]> {
        return this.http.get(`api/fusion/${fusionTypeID}/configurations/${fusionID}/filters`)
            .toPromise()
            .then(response => <FusionFilter[]>response.json())
            .catch(err =>  this.handleError(err));
    }

    getFusionAttributeTypeList(fusionID: number): Promise<FusionAttributeType[]> {
        return this.http.get(`form/getfusionattributetypes?fusionID=${fusionID}`)
            .toPromise()
            .then(response => <FusionAttributeType[]>response.json())
            .catch(err => this.handleError(err));
    }

    postFusionConfigurationFilter(filter: FusionFilter): Promise<any> {
        return this.http.post('form/fusionfilter', filter)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    putFusionConfigurationFilter(filter: FusionFilter): Promise<any> {
        return this.http.put('form/fusionfilter', filter)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getFusionQueryAttributeTypes(typeid: number, id: number, query: string = ''): Promise<FusionQueryAttributeType[]> {
        return this.http.get(`services/fusion/${typeid}/configurations/${id}/queryattributetypes?${query}`)
            .toPromise()
            .then(response => <FusionQueryAttributeType[]>response.json())
            .catch(err => this.handleError(err));
    }

    getFusionTypeStyle(fusionID: number): Promise<ObjectStyle> {
        return this.http.get(`api/fusiontype/${fusionID}/style`)
            .toPromise()
            .then(response => <ObjectStyle>response.json())
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

    postFusionAttributeType(fusionAttributeType: FusionAttributeType): Promise<any>{
        return this.http.post('form/FusionAttributeType', fusionAttributeType)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    putFusionAttributeType(fusionAttributeType: FusionAttributeType): Promise<any> {
        return this.http.put('form/FusionAttributeType', fusionAttributeType)
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
        return this.http.get(`services/fusion/${fusionId}/attributetypes?$orderby=Name`)
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

    getFusionRuleItems(id: number): Promise<FusionRuleItem[]> {
        return this.http.get(`api/fusion/${id}/FusionRuleItems`)
            .toPromise()
            .then(response => <FusionRuleItem[]>response.json())
            .catch(err => this.handleError(err));
    }

    getFusionRuleStepMappings(id: number): Promise<FusionRuleMapping[]> {
        return this.http.get(`api/fusion/${id}/FusionRuleStepMappings`)
            .toPromise()
            .then(response => <FusionRuleMapping[]>response.json())
            .catch(err => this.handleError(err));
    }

    getFusionExecutionErrors(executionId: number): Promise<FusionExecutionError[]> {
        return this.http.get(`services/fusion/executionerrors?$filter=ExecutionID%20eq%20${executionId}&$orderby=Date%20desc`)
            .toPromise()
            .then(response => <FusionExecutionError[]>response.json())
            .catch(err => this.handleError(err));
    }

    getFusionExecutionResults(executionId: number): Promise<FusionExecutionResult[]> {
        return this.http.get(`services/fusion/executions/${executionId}/results`)
            .toPromise()
            .then(response => <FusionExecutionResult[]>response.json().results)
            .catch(err => this.handleError(err));
    }

    downloadFusionManualLoadTemplate(fusionId: number, fusionTypeId: number, fusionAttributeTypeId: number) {
        window.location.assign(`internal/fusion/${fusionTypeId}/configurations/${fusionId}/template/${fusionAttributeTypeId}`);
    }

    getEditFusionRule(id: number): Promise<FusionRuleEditorModel> {
        return this.http.get(`form/GetEditFusionRule?id=${id}`)
            .toPromise()
            .then(response => <FusionRuleEditorModel>response.json())
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

    getAddFusionRule(typeID: number): Promise<FusionAttributeType[]> {
        return this.http.get(`form/GetAddFusionRule?typeID=${typeID}`)
            .toPromise()
            .then(response => <FusionAttributeType[]>response.json())
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

}