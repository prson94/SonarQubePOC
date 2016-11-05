import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { Relationship, RelationshipDetail, ObjectRelationship, RelatedItem, ObjectRelationshipCount, PossibleTechnicalRelationship, RelationshipRole } from '../models/relationship.model';
import { JsonResult } from '../models/jsonresult.model';
import { DropdownOption } from '../models/dropdown.model';
import { HierarchyArtifactsModel, HierarchyArtifactItem, HierarchyPostModel } from '../models/relations.model';

@Injectable()
export class RelationshipsService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getRelations(): Promise<Relationship[]> {
        return this.http.get('relations/_intersectTypes')
            .toPromise()
            .then(response => <Relationship[]>response.json())
            .catch(err => this.handleError(err));
    }

    getRelation(id: number): Promise<RelationshipDetail> {
        return this.http.get(`form/IntersectType_FormData?id=${id}`)
            .toPromise()
            .then(response => <RelationshipDetail>response.json())
            .catch(err => this.handleError(err));
    }

    getPossibleTechnicalRelations(id: number): Promise<PossibleTechnicalRelationship[]> {
        return this.http.get(`relations/GetPossibleRelationshipsObjectByIntersect?id=${id}`)
            .toPromise()
            .then(response => <PossibleTechnicalRelationship[]>response.json())
            .catch(err => this.handleError(err));
    }

    getObjectRelations(objectType: string, objectId: number): Promise<ObjectRelationship[]> {
        return this.http.get(`/api/${objectType}/${objectId}/relationshipTypes`)
            .toPromise()
            .then(response => <ObjectRelationship[]>response.json())
            .catch(err => this.handleError(err));
    }

    getRelatedObjects(objectType: string, objectId: number): Promise<RelatedItem[]> {
        return this.http.get(`/api/RelationshipObjectsByType?type=${objectType}&id=${objectId}`)
            .toPromise()
            .then(response => <RelatedItem[]>response.json())
            .catch(err => this.handleError(err));
    }

    deleteRelationship(id: number): Promise<JsonResult>{
        return this.deleteDynamicWithResult(this.http, 'intersecttype', id);
    }

    saveRelationship(relationship: RelationshipDetail): Promise<JsonResult> {
        if (relationship.ID == undefined || !relationship.ID) {
            return this.postDynamic(this.http, 'intersecttype', relationship);
        }
        return this.putDynamic(this.http, 'intersecttype', relationship);
    }

    getRelationshipPredicates(subject: string, subjectId: number, object?: string, objectId?: number, predicateId?: number): Promise<DropdownOption[]> {
        let url = `form/IntersectType_PredicateOptions?subject=${subject}&subjectID=${subjectId}`;
        if (object != undefined)
            url = url += `&object=${object}`;
        if (objectId != undefined)
            url = url += `&objectID=${objectId}`;
        if (predicateId != undefined)
            url = url += `&predicateID=${predicateId}`;
        return this.http.get(url)
            .toPromise()
            .then(response => <DropdownOption[]>response.json())
            .catch(err => this.handleError(err));
    }

    getSide1Options(): Promise<DropdownOption[]> {
        return this.http.get('form/IntersectType_Side1Options')
            .toPromise()
            .then(response => <DropdownOption[]>response.json())
            .catch(err => this.handleError(err));
    }

    getSide2Options(id: number, type: string, selectedId?: number, selectedType?: string, predicateId?: number): Promise<DropdownOption[]> {
        let url = `form/IntersectType_Side2Options?id=${id}&type=${type}`;
        if (selectedId != undefined)
            url = url += `&side2ID=${selectedId}`;
        if (selectedType != undefined)
            url = url += `&side2Type=${selectedType}`;
        if (predicateId != undefined)
            url = url += `&predicateID=${predicateId}`;

        return this.http.get(url)
            .toPromise()
            .then(response => <DropdownOption[]>response.json())
            .catch(err => this.handleError(err));
    }

    getRelationshipCounts(objectType: string, objectId: number): Promise<ObjectRelationshipCount[]> {        
        return this.http.get(`/api/${objectType}/${objectId}/relationships/counts`)
            .toPromise()
            .then(response => <ObjectRelationshipCount[]>response.json())
            .catch(err => this.handleError(err));
        
    }

    getObjectRelationships(objectType: string, objectId: number, targetType: string, targetTypeId: number, intersectTypeID: number, criticalOnly?: boolean): Promise<any> {
        criticalOnly = (criticalOnly == undefined ? false : criticalOnly);

        return this.http.get(`/api/${objectType}/${objectId}/relationships/${targetType}/${targetTypeId}/${intersectTypeID}/${criticalOnly}`)
            .toPromise()
            .then(response => <any[]>response.json())
            .catch(err => this.handleError(err));
    }

    getTechnicalRelationships(objectType: string, objectId: number): Promise<any> {        
        return this.http.get(`/api/${objectType}/${objectId}/relations`)
            .toPromise()
            .then(response => <any[]>response.json())
            .catch(err => this.handleError(err));
    }

    exportObjectRelationshipsToExcel(objectType: string, objectId: number, targetType: string, targetTypeId: number, intersectTypeID: number, criticalOnly?: boolean){
        criticalOnly = (criticalOnly == undefined ? false : criticalOnly);

        window.location.assign(`/api/export/${objectType}/${objectId}/relationships/${targetType}/${targetTypeId}/${intersectTypeID}/excel.xls`);        
    }

    deleteRelationshipItem(id: number): Promise<any> {
        let url = `/api/relationships/${id}`;

        return this.http
            .delete(url)
            .toPromise()
            .then(response => response)
            .catch(err => this.handleError(err));
    }

    deleteHierarchyItem(id: number) {
        return this.http.delete(`relations/hierarchy/delete/${id}`)
            .toPromise()
            .catch(err => this.handleError(err));
    }

    getHierarchyArtifacts(model: HierarchyArtifactsModel): Promise<HierarchyArtifactItem[]> {
        return this.http.post('relations/hierarchy/artifacts', model)
            .toPromise()
            .then(response => <HierarchyArtifactItem[]>response.json())
            .catch(err => this.handleError(err));
    }

    postHierarchy(model: HierarchyPostModel): Promise<any> {
        return this.http.post('relations/hierarchy/save', model)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getRelationshipRoles(): Promise<RelationshipRole[]> {
        return this.http.get('relations/IntersectRoles')
            .toPromise()
            .then(response => <RelationshipRole[]>response.json())
            .catch(err => this.handleError(err));
    }

    deleteRelationshipRole(id: number): Promise<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'relationshiprole', id);
    }

    saveRelationshipRole(relationshipRole: RelationshipRole): Promise<JsonResult> {
        if (relationshipRole.ID == undefined || !relationshipRole.ID) {
            return this.postDynamic(this.http, 'relationshiprole', relationshipRole);
        }
        return this.putDynamic(this.http, 'relationshiprole', relationshipRole);
    }
}