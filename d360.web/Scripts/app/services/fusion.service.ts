///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { BaseService } from './base.service';
import { MessagesService } from './index';
import { JsonResult, FormHelper } from '../models/form.model';
import { FusionType, FusionAttributeType, FusionConfiguration, FusionFilter, ObjectStyle, Fusion, FusionConfigurationDetails, FusionAgentExecutionStats, FusionWorkerExecution, FusionPromotionExecutionStats, FusionSummaryStats } from '../models/fusion.model';
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

    getFusionConfigurations(): Promise<Fusion[]> {
        return this.http.get(`services/fusion/configurations?$orderby=FusionType,Name`)
            .toPromise()
            .then(response => <Fusion[]>response.json())
            .catch(err => this.handleError(err));
    }

    getFusionAgentHistory(maxRows?:number): Promise<FusionAgentExecutionStats[]> {
        return this.http.get(`services/fusion/agenthistory?$top=${maxRows? maxRows:'100'}&$orderby=DateStarted%20desc`)
            .toPromise()
            .then(response => <FusionAgentExecutionStats[]>response.json())
            .catch(err => this.handleError(err));
    }

    getFusionWorkerExecutionHistory(maxRows?: number): Promise<FusionWorkerExecution[]> {
        return this.http.get(`services/fusion/executionhistory?$top=${maxRows ? maxRows : '100'}&$orderby=DateStarted%20desc`)
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

    getFusionStatsSummary(): Promise<FusionSummaryStats> {
        return this.http.get('api/fusion/statistics')
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


    getFusionFusionAttributeTypes(fusionId: number): Promise<FusionAttributeType[]>{
        return this.http.get(`services/fusion/${fusionId}/attributetypes?$orderby=Name`)
            .toPromise()
            .then(response => <FusionAttributeType[]>response.json())
            .catch(err => this.handleError(err));
    }
}