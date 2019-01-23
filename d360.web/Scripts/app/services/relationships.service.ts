import { Injectable } from '@angular/core';
import { Headers, Http, Response, ResponseContentType } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { RelationshipType, RelationshipDetail, ObjectRelationship, RelatedItem, ObjectRelationshipCount, PossibleTechnicalRelationship, RelationshipRole } from '../models/relationship.model';
import { JsonResult } from '../models/jsonresult.model';
import { DropdownOption } from '../models/dropdown.model';
import { HierarchyArtifactsModel, HierarchyArtifactItem, HierarchyPostModel } from '../models/relations.model';

@Injectable()
export class RelationshipsService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getRelationshipTypes(): Promise<RelationshipType[]> {
        return this.http.get('api/v2/relationships/types')
            .toPromise()
            .then(response => <RelationshipType[]>response.json())
            .catch(err => this.handleError(err));
    }

    getRelationshipTypesById(id: number, type: string): Promise<RelationshipType[]> {
        return this.http.get(`api/v2/relationships/types/${id}/${type}`)
            .toPromise()
            .then(response => <RelationshipType[]>response.json())
            .catch(err => this.handleError(err));
    }

    exportRelationshipTypeItems(relType: RelationshipType) {
        this.http.get(`relations/_intersectTypeItems/${relType.ID}/excel.xls`, { responseType: ResponseContentType.Blob }).subscribe(data => this.downloadFile(data, 'relationship type items'));
    }

    exportRelationshipTypes() {        
        this.http.get('relations/_intersectTypes/excel.xls', { responseType: ResponseContentType.Blob }).subscribe(data => this.downloadFile(data, 'relationship types'));              
    }

    downloadFile(data: Response, name: string) {
        var filename = `${name} ${new Date().toDateString()}.xlsx`;
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

    getRelatedObjects(objectType: string, objectId: number, intersectTypeId: number): Promise<RelatedItem[]> {
        return this.http.get(`/api/RelationshipObjectsByType?type=${objectType}&id=${objectId}&intersectTypeId=${intersectTypeId}`)
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

    getCardinalityOptions(): Promise<DropdownOption[]> {
        return this.http.get('form/IntersectType_CardinalityOptions')
            .toPromise()
            .then(response => <DropdownOption[]>response.json())
            .catch(err => this.handleError(err));
    }

    getSubjectOptions(): Promise<DropdownOption[]> {
        return this.http.get('form/IntersectType_SubjectOptions')
            .toPromise()
            .then(response => <DropdownOption[]>response.json())
            .catch(err => this.handleError(err));
    }

    getObjectOptions(id: number, type: string, selectedId?: number, selectedType?: string, predicateId?: number): Promise<DropdownOption[]> {
        let url = `form/IntersectType_ObjectOptions?id=${id}&type=${type}`;
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

    getObjectRelationships(objectType: string, objectId: number, targetType: string, targetTypeId: number, intersectTypeID: number, includeInverse: boolean = true): Promise<any> {
        return this.http.get(`/api/${objectType}/${objectId}/relationships/${targetType}/${targetTypeId}/${intersectTypeID}?includeInverse=${includeInverse}`)
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

    exportObjectRelationshipsToExcel(objectType: string, objectId: number, targetType: string, targetTypeId: number, intersectTypeID: number, queryString: string, criticalOnly?: boolean){
        criticalOnly = (criticalOnly == undefined ? false : criticalOnly);

        window.location.assign(`/api/export/${objectType}/${objectId}/relationships/${targetType}/${targetTypeId}/${intersectTypeID}/excel.xls?${queryString}`);        
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