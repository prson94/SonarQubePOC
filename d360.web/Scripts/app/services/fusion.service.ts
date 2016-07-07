///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { BaseService } from './base.service';
import { MessagesService } from './index';
import { JsonResult, FormHelper } from '../models/form.model';
import { FusionType, FusionAttributeType, FusionConfiguration, FusionFilter } from '../models/fusion.model';
import { TreeNode } from 'primeng/primeng';
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

}