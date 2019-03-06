import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import {
    IResponsibilityTypeService,
    ResponsibilityType,
    ResponsibilityTypeGroup,
    ResponsibilityTypeRelation,
    ResponsibilityTypeCount,
    ResourceResponsibilityTypeCount,
    ResponsibilityTypeRelationRule,
    ResponsibilityTypeRelationRuleSummary,
    ResponsibilityTypeRelationRuleFormData,
    ResponsibilityTypeRelationRuleDefinitionWhenItem,
    ResponsibilityTypeRelationRuleDefinitionWhenTestRow,
    ResponsibilityTypeRelationRuleDefinitionThenItem,
    ResponsibilityTypeRelationRuleDefinitionThenTestRow,
    ResponsibilityTypeRelation_FormData
} from '../models/responsibility-type.model';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { SelectItem } from "primeng/primeng";

@Injectable()
export class ResponsibilityTypeService extends BaseService implements IResponsibilityTypeService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getResponsibilityTypes(): Promise<ResponsibilityType[]> {
        return this.http.get('api/ownership/types')
            .toPromise()
            .then(response => <ResponsibilityType[]>response.json())
            .catch(err => this.handleError(err));
    }

    getAdminResponsibilityTypes(): Promise<ResponsibilityType[]> {
        return this.http.get('api/ownership/admintypes')
            .toPromise()
            .then(response => <ResponsibilityType[]>response.json())
            .catch(err => this.handleError(err));
    }

    getResponsibilityType(id: number, group: ResponsibilityTypeGroup = ResponsibilityTypeGroup.People): Promise<ResponsibilityType> {
        return this.http.get(`form/ResponsibilityType?id=${id}&group=${group}`)
            .toPromise()
            .then(r => r.json())
            .then(r => {                
                let t = new ResponsibilityType();
                t = r.model;
                t.AllocationsList = r.allocations;
                t.ResponsibilityTypeRelations = r.selectedAllocations;
                if (t.ResponsibilityTypeRelations == null)
                    t.ResponsibilityTypeRelations = [];
                return t;
            })
            .catch(err => this.handleError(err));
    }

    putResponsibilityType(responsibilityType: ResponsibilityType): Promise<any> {
        return this.http.put(`form/ResponsibilityType`, responsibilityType)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    postResponsibilityType(responsibilityType: ResponsibilityType): Promise<any> {
        return this.http.post(`form/ResponsibilityType`, responsibilityType)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    deleteResponsibilityType(id: number): Promise<any> {
        return this.http.delete(`form/DeleteResponsibilityTypeByID?id=${id}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getResponsibilityTypeBreakdown(): Promise<ResponsibilityTypeCount[]> {
        return this.http.get('queries/ResponsibilityTypeBreakdown')
            .toPromise()
            .then(response => <ResponsibilityTypeCount[]>response.json())
            .catch(err => this.handleError(err));
    }

    getResourceResponsibilityByType(responsibilityTypeId: number): Promise<ResourceResponsibilityTypeCount[]> {
        return this.http.get(`queries/${responsibilityTypeId}/ResourcesByResponsibilityType`)
            .toPromise()
            .then(response => <ResourceResponsibilityTypeCount[]>response.json())
            .catch(err => this.handleError(err));
    }

    getRelationFormData(): Promise<ResponsibilityTypeRelation_FormData> {
        return this.http.get(`form/ResponsibilityTypeRelation_FormData`)
            .toPromise()
            .then(response => <ResponsibilityTypeRelation_FormData>response.json())
            .catch(err => this.handleError(err));
    }

    getResponsibilityTypesByObject(type: string, id: number): Promise<any> {
        let uri = `api/ownership/${type}/${id}/responsibilitytypes`;
        if (type.toLowerCase() == 'fusion') {
            uri = `api/ownership/fusion/${id}/fusionresponsibilitytypes`
        } 

        return this.http.get(uri)
            .toPromise()
            .then(response => <any>response.json())
            .catch(err => this.handleError(err));
    }

    getRelationsByAssetType(id: number): Promise<ResponsibilityTypeRelation[]> {
        return this.http.get(`api/ownership/types/asset/${id}/relations`)
            .toPromise()
            .then(response => <ResponsibilityTypeRelation[]>response.json())
            .catch(err => this.handleError(err));
    }

    getRelationsByObjectType(type: string, id: number): Promise<ResponsibilityTypeRelation[]> {
        return this.http.get(`api/ownership/${type}/${id}/responsibilitytypes`)
            .toPromise()
            .then(response => <ResponsibilityTypeRelation[]>response.json())
            .catch(err => this.handleError(err));
    }

    getRelationsByResponsibilityType(id: number): Promise<ResponsibilityTypeRelation[]> {
        return this.http.get(`api/ownership/types/${id}/relations`)
            .toPromise()
            .then(response => <ResponsibilityTypeRelation[]>response.json())
            .catch(err => this.handleError(err));
    }

    putRelation(rule: ResponsibilityTypeRelation): Promise<any> {
        return this.http.put(`form/ResponsibilityTypeRelation`, rule)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    postRelation(rule: ResponsibilityTypeRelation): Promise<any> {
        return this.http.post(`form/ResponsibilityTypeRelation`, rule)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    deleteRelation(relation: ResponsibilityTypeRelation): Promise<any> {
        return this.http.delete(`form/ResponsibilityTypeRelation?responsibilityTypeId=${relation.ResponsibilityTypeID}&type=${relation.ObjectType}&typeId=${relation.ObjectID}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }


    getResponsibilityTypeRelationRule(id: number): Promise<ResponsibilityTypeRelationRule> {
        return this.http.get(`form/ResponsibilityTypeRelationRule?id=${id}`)
            .toPromise()
            .then(r => <ResponsibilityTypeRelationRule>r.json())
            .catch(err => this.handleError(err));
    }

    getRelationOptionsByResponsibilityType(id: number): Promise<SelectItem[]> {
        return this.http.get(`form/RelationsByResponsibilityType?id=${id}`)
            .toPromise()
            .then(response => <SelectItem[]>response.json())
            .catch(err => this.handleError(err));
    }

    getRelationRuleFormData(type: string, id: number): Promise<ResponsibilityTypeRelationRuleFormData> {
        return this.http.get(`form/ResponsibilityTypeRelationRule_FormData?type=${type}&id=${id}`)
            .toPromise()
            .then(response => <ResponsibilityTypeRelationRuleFormData>response.json())
            .catch(err => this.handleError(err));
    }

    getRelationRuleFormDataRelationshipsForDropdown(type: string, id: number, intersectTypeId: number): Promise<SelectItem[]> {
        return this.http.get(`form/ResponsibilityTypeRelationRuleRelationships_FormData?type=${type}&id=${id}&intersectTypeID=${intersectTypeId}`)
            .toPromise()
            .then(response => <SelectItem[]>response.json())
            .catch(err => this.handleError(err));
    }

    getRelationRulesByResponsibilityType(id: number): Promise<ResponsibilityTypeRelationRuleSummary[]> {
        return this.http.get(`api/ownership/types/${id}/rules`)
            .toPromise()
            .then(response => <ResponsibilityTypeRelationRuleSummary[]>response.json())
            .catch(err => this.handleError(err));
    }

    putRule(rule: ResponsibilityTypeRelationRule): Promise<any> {
        return this.http.put(`form/ResponsibilityTypeRelationRule`, rule)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    postRule(rule: ResponsibilityTypeRelationRule): Promise<any> {
        return this.http.post(`form/ResponsibilityTypeRelationRule`, rule)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    deleteRule(id: number): Promise<any> {
        return this.http.delete(`form/DeleteResponsibilityTypeRelationRuleByID?id=${id}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    deleteDate(id: number): Promise<any> {
        return this.http.delete(`form/DeleteResponsibilityTypeRelationRuleDateByID?id=${id}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }
    
    testWhen(rule: ResponsibilityTypeRelationRule): Promise<ResponsibilityTypeRelationRuleDefinitionWhenTestRow[]> {
        return this.http.post(`form/ResponsibilityTypeRelationRule_WhenTest`, rule)
            .toPromise()
            .then(response => <ResponsibilityTypeRelationRuleDefinitionWhenTestRow[]>response.json())
            .catch(err => this.handleError(err));
    }

    testThen(rule: ResponsibilityTypeRelationRule): Promise<ResponsibilityTypeRelationRuleDefinitionThenTestRow[]> {
        return this.http.post(`form/ResponsibilityTypeRelationRule_ThenTest`, rule)
            .toPromise()
            .then(response => <ResponsibilityTypeRelationRuleDefinitionThenTestRow[]>response.json())
            .catch(err => this.handleError(err));
    }
}

