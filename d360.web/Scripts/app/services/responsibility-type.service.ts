import { Injectable } from '@angular/core';
import {
    IResponsibilityTypeService,
    ResponsibilityType,    
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
import { SelectItem } from "primeng/api";
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';

@Injectable()
export class ResponsibilityTypeService extends BaseObservableService implements IResponsibilityTypeService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    getResponsibilityTypes(): Observable<ResponsibilityType[]> {
        return this.http.get('api/ownership/types')
            .pipe(
                map((response) => <ResponsibilityType[]>response),
                catchError((err) => this.handleError(err))
            );
    }

    getAdminResponsibilityTypes(uid?: string): Observable<ResponsibilityType[]> {
        let uidstring = "";
        if (uid)
            uidstring = `/${uid}`
        return this.http.get(`api/v2/responsibilities/types${uidstring}`)
            .pipe(
                map((response) => <ResponsibilityType[]>response),
                catchError((err) => this.handleError(err))
            );
    }


    deleteResponsibilityRulesForType(ruleUid?: string, responsibilityTypeUid?: string): Observable<any> {
        var model = [];
        model.push({ uid: ruleUid })

        const httpHeaders = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' }),
            body: model
        };
        return this.http.delete(`api/v2/responsibilities/types/${responsibilityTypeUid}/ownershiprules`, httpHeaders)
            .pipe(
                map((response) => <any[]>response),
                catchError((err) => this.handleError(err))
            );
    }

    getAdminResponsibilityTypeDetails(uid: string): Observable<any> {
        return this.http.get(`api/v2/responsibilities/type/${uid}`)
            .pipe(
                map((response) => <ResponsibilityType[]>response),
                catchError((err) => this.handleError(err))
            );
    }

    getResponsibilityType(id: number): Observable<ResponsibilityType> {
        return this.http.get(`form/ResponsibilityType?id=${id}`)
            .pipe(
                map(r => <any>r),

                map(r => {
                    let t = new ResponsibilityType();
                    t = r.model;
                    t.AllocationsList = r.allocations;
                    t.ResponsibilityTypeRelations = r.selectedAllocations;
                    if (t.ResponsibilityTypeRelations == null)
                        t.ResponsibilityTypeRelations = [];
                    return t;
                }),
                catchError((err) => this.handleError(err))
            );
    }

    putResponsibilityType(responsibilityType: ResponsibilityType): Observable<any> {
        return this.http.put(`form/ResponsibilityType`, responsibilityType)
            .pipe(
                map((response) => response),
                catchError((err) => this.handleError(err))
            );
    }

    postResponsibilityType(responsibilityType: ResponsibilityType): Observable<any> {
        return this.http.post(`form/ResponsibilityType`, responsibilityType)
            .pipe(
                map((response) => response),
                catchError((err) => this.handleError(err))
            );
    }

    deleteResponsibilityType(Uid: string, Cascade: boolean = true): Observable<any> {
        const httpHeaders = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' }), body: {Uid,Cascade}
        };        
        return this.http.delete(`api/v2/responsibilities/types`, httpHeaders)
            .pipe(
                map((response) => response),
                catchError((err) => this.handleError(err))
            );
    }

    getResponsibilityTypeBreakdown(): Observable<ResponsibilityTypeCount[]> {
        return this.http.get('queries/ResponsibilityTypeBreakdown')
            .pipe(
                map((response) => <ResponsibilityTypeCount[]>response),
                catchError((err) => this.handleError(err))
            );
    }

    getResourceResponsibilityByType(responsibilityTypeUid: string): Observable<ResourceResponsibilityTypeCount[]> {
        return this.http.get(`queries/${responsibilityTypeUid}/ResourcesByResponsibilityType`)
            .pipe(
                map((response) => <ResourceResponsibilityTypeCount[]>response),
                catchError((err) => this.handleError(err))
            );
    }

    getRelationFormData(): Observable<ResponsibilityTypeRelation_FormData> {
        return this.http.get(`form/ResponsibilityTypeRelation_FormData`)
            .pipe(
                map((response) => <ResponsibilityTypeRelation_FormData>response),
                catchError((err) => this.handleError(err))
            );
    }

    getResponsibilityTypesByObject(type: string, id: number): Observable<any> {
        let uri = `api/ownership/${type}/${id}/responsibilitytypes`;
        if (type.toLowerCase() == 'fusion') {
            uri = `api/ownership/fusion/${id}/fusionresponsibilitytypes`
        }

        return this.http.get(uri)
            .pipe(
                map((response) => <any>response),
                catchError((err) => this.handleError(err))
            );
    }

    getRelationsByAssetType(id: number): Observable<ResponsibilityTypeRelation[]> {
        return this.http.get(`api/ownership/types/asset/${id}/relations`)
            .pipe(
                map((response) => <ResponsibilityTypeRelation[]>response),
                catchError((err) => this.handleError(err))
            );
    }

    getRelationsByObjectType(type: string, id: number): Observable<ResponsibilityTypeRelation[]> {
        return this.http.get(`api/ownership/${type}/${id}/responsibilitytypes`)
            .pipe(
                map((response) => <ResponsibilityTypeRelation[]>response),
                catchError((err) => this.handleError(err))
            );
    }

    getRelationsByResponsibilityType(id: number): Observable<ResponsibilityTypeRelation[]> {
        return this.http.get(`api/ownership/types/${id}/relations`)
            .pipe(
                map((response) => <ResponsibilityTypeRelation[]>response),
                catchError((err) => this.handleError(err))
            );
    }

    putRelation(rule: ResponsibilityTypeRelation): Observable<any> {
        return this.http.put(`form/ResponsibilityTypeRelation`, rule)
            .pipe(
                map((response) => response),
                catchError((err) => this.handleError(err))
            );
    }

    postRelation(rule: ResponsibilityTypeRelation): Observable<any> {
        return this.http.post(`form/ResponsibilityTypeRelation`, rule)
            .pipe(
                map((response) => response),
                catchError((err) => this.handleError(err))
            );
    }

    deleteRelation(relation: ResponsibilityTypeRelation): Observable<any> {
        return this.http.delete(`form/ResponsibilityTypeRelation?responsibilityTypeId=${relation.ResponsibilityTypeID}&type=${relation.ObjectType}&typeId=${relation.ObjectID}`)
            .pipe(
                map((response) => response),
                catchError((err) => this.handleError(err))
            );
    }


    getResponsibilityTypeRelationRule(id: number): Observable<ResponsibilityTypeRelationRule> {
        return this.http.get(`form/ResponsibilityTypeRelationRule?id=${id}`)
            .pipe(
                map((r) => <ResponsibilityTypeRelationRule>r),
                catchError((err) => this.handleError(err))
            );
    }

    getRelationOptionsByResponsibilityType(id: number): Observable<SelectItem[]> {
        return this.http.get(`form/RelationsByResponsibilityType?id=${id}`)
            .pipe(
                map((response) => <SelectItem[]>response),
                catchError((err) => this.handleError(err))
            );
    }

    getRelationRuleFormData(type: string, id: number): Observable<ResponsibilityTypeRelationRuleFormData> {
        return this.http.get(`form/ResponsibilityTypeRelationRule_FormData?type=${type}&id=${id}`)
            .pipe(
                map((response) => <ResponsibilityTypeRelationRuleFormData>response),
                catchError((err) => this.handleError(err))
            );
    }

    getRelationRuleFormDataRelationshipsForDropdown(type: string, id: number, intersectTypeId: number): Observable<SelectItem[]> {
        return this.http.get(`form/ResponsibilityTypeRelationRuleRelationships_FormData?type=${type}&id=${id}&intersectTypeID=${intersectTypeId}`)
            .pipe(
                map((response) => <SelectItem[]>response),
                catchError((err) => this.handleError(err))
            );
    }

    getRelationRulesByResponsibilityType(id: number): Observable<ResponsibilityTypeRelationRuleSummary[]> {
        return this.http.get(`api/ownership/types/${id}/rules`)
            .pipe(
                map((response) => <ResponsibilityTypeRelationRuleSummary[]>response),
                catchError((err) => this.handleError(err))
            );
    }

    putRule(rule: ResponsibilityTypeRelationRule): Observable<any> {
        return this.http.put(`form/ResponsibilityTypeRelationRule`, rule)
            .pipe(
                map((response) => response),
                catchError((err) => this.handleError(err))
            );
    }

    postRule(rule: ResponsibilityTypeRelationRule): Observable<any> {
        return this.http.post(`form/ResponsibilityTypeRelationRule`, rule)
            .pipe(
                map((response) => response),
                catchError((err) => this.handleError(err))
            );
    }

    deleteDate(id: number): Observable<any> {
        return this.http.delete(`form/DeleteResponsibilityTypeRelationRuleDateByID?id=${id}`)
            .pipe(
                map((response) => response),
                catchError((err) => this.handleError(err))
            );
    }

    testWhen(rule: ResponsibilityTypeRelationRule): Observable<ResponsibilityTypeRelationRuleDefinitionWhenTestRow[]> {
        return this.http.post(`form/ResponsibilityTypeRelationRule_WhenTest`, rule)
            .pipe(
                map((response) => <ResponsibilityTypeRelationRuleDefinitionWhenTestRow[]>response),
                catchError((err) => this.handleError(err))
            );
    }

    testThen(rule: ResponsibilityTypeRelationRule): Observable<ResponsibilityTypeRelationRuleDefinitionThenTestRow[]> {
        return this.http.post(`form/ResponsibilityTypeRelationRule_ThenTest`, rule)
            .pipe(
                map((response) => <ResponsibilityTypeRelationRuleDefinitionThenTestRow[]>response),
                catchError((err) => this.handleError(err))
            );
    }
}