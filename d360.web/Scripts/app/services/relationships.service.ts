import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { MessagesObservableService } from './messages-observable.service';
import { BaseObservableService } from './baseObservable.service';
import { RelationshipType, RelationshipDetail, ObjectRelationship, RelatedItem, ObjectRelationshipCount, PossibleTechnicalRelationship } from '../models/relationship.model';
import { JsonResult } from '../models/jsonresult.model';
import { DropdownOption } from '../models/dropdown.model';
import { Observable } from 'rxjs';
import { ApiResult } from '../models/apiresult.model';


@Injectable()
export class RelationshipsService extends BaseObservableService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    getRelationshipTypes(): Observable<RelationshipType[]> {
        return this.http.get('api/v2/relationships/types?state=1')
            .pipe(
                map(response => <RelationshipType[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getRelationshipTypesById(id: number, type: string): Observable<RelationshipType[]> {
        return this.http.get(`api/v2/relationships/types/${id}/${type}`)
            .pipe(
                map(response => <RelationshipType[]> response),
                catchError(err => this.handleError(err))
            );
    }

    saveRelationships(intersectTypeUid: number, model: any[]): Observable<ApiResult[]> {
        return this.http.post(`api/v2/relationships/${intersectTypeUid}/?triggerWorkflow=true`, model).pipe(
            map(response => response),
            catchError(err => this.handleError(err, true))
        );
    }

    deleteRelationshipV2(intersectTypeUid: number, model: any[]): Observable<ApiResult[]> {
        const httpHeaders = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' }), body: model
        };
        return this.http.delete(`api/v2/relationships/${intersectTypeUid}/?triggerWorkflow=true`, httpHeaders).pipe(
            map(response => response),
            catchError(err => this.handleError(err, true))
        );
    }
    exportRelationshipTypeItems(relType: RelationshipType) {
        this.http.get(`api/v2/relationships/export/${relType.Uid}`, { responseType: 'blob' }).subscribe(data => this.downloadFile(data, 'relationship type items'));
    }

    exportRelationshipTypes() {        
        this.http.get('api/v2/relationships/export/types', { responseType: 'blob' }).subscribe(data => this.downloadFile(data, 'relationship types'));              
    }

    downloadFile(data: Blob, name: string) {
        var filename = `${name} ${new Date().toDateString()}.xlsx`;
        if (window.navigator.msSaveOrOpenBlob) {
            window.navigator.msSaveOrOpenBlob(data, filename);
        }
        else {
            var url = window.URL.createObjectURL(data);
            var anchor = document.createElement("a");
            anchor.setAttribute("style", "display:none;");
            document.body.appendChild(anchor);
            anchor.setAttribute("download", filename);
            anchor.href = url;
            anchor.click();
        }
    }

    getRelation(id: number): Observable<RelationshipDetail> {
        return this.http.get(`form/IntersectType_FormData?id=${id}`)
            .pipe(
                map(response => <RelationshipDetail>response),
                catchError(err => this.handleError(err))
            );
    }

    getPossibleTechnicalRelations(id: number): Observable<PossibleTechnicalRelationship[]> {
        return this.http.get(`relations/GetPossibleRelationshipsObjectByIntersect?id=${id}`)
            .pipe(
                map(response => <PossibleTechnicalRelationship[]>response),
                catchError(err=>this.handleError(err))
            );
    }

    getObjectRelations(objectType: string, objectId: number): Observable<ObjectRelationship[]> {
        return this.http.get(`/api/${objectType}/${objectId}/relationshipTypes`)
            .pipe(
            map(response => <ObjectRelationship[]>response),
            catchError(err => this.handleError(err))
                );
    }

    getRelatedObjects(objectType: string, objectId: number, intersectTypeId: number): Observable<RelatedItem[]> {
        return this.http.get(`/api/RelationshipObjectsByType?type=${objectType}&id=${objectId}&intersectTypeId=${intersectTypeId}`)
            .pipe(
                map(response => <RelatedItem[]>response),
                catchError(err=>this.handleError(err))
            );
    }

    deleteRelationship(id: number): Observable<JsonResult>{
        return this.deleteDynamicWithResult(this.http, 'intersecttype', id);
    }

    saveRelationship(relationship: RelationshipDetail): Observable<JsonResult> {
        if (relationship.ID == undefined || !relationship.ID) {
            return this.postDynamic(this.http, 'intersecttype', relationship);
        }
        return this.putDynamic(this.http, 'intersecttype', relationship);
    }

    getRelationshipPredicates(subject: string, subjectId: number, object?: string, objectId?: number, predicateId?: number): Observable<DropdownOption[]> {
        let url = `form/IntersectType_PredicateOptions?subject=${subject}&subjectID=${subjectId}`;
        if (object != undefined)
            url = url += `&object=${object}`;
        if (objectId != undefined)
            url = url += `&objectID=${objectId}`;
        if (predicateId != undefined)
            url = url += `&predicateID=${predicateId}`;
        return this.http.get(url)
            .pipe(
                map(response => <DropdownOption[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getCardinalityOptions(): Observable<DropdownOption[]> {
        return this.http.get('form/IntersectType_CardinalityOptions')
            .pipe(
                map(response => <DropdownOption[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getSubjectOptions(): Observable<DropdownOption[]> {
        return this.http.get('form/IntersectType_SubjectOptions')
            .pipe(
                map(response => <DropdownOption[]>response),
                catchError(err=> this.handleError(err))
            );
    }

    getObjectOptions(id: number, type: string, selectedId?: number, selectedType?: string, predicateId?: number): Observable<DropdownOption[]> {
        let url = `form/IntersectType_ObjectOptions?id=${id}&type=${type}`;
        if (selectedId != undefined)
            url = url += `&side2ID=${selectedId}`;
        if (selectedType != undefined)
            url = url += `&side2Type=${selectedType}`;
        if (predicateId != undefined)
            url = url += `&predicateID=${predicateId}`;

        return this.http.get(url)
            .pipe(
            map(response => <DropdownOption[]>response),
            catchError(err=> this.handleError(err))
        );
    }

    getRelationshipCounts(objectType: string, objectId: number): Observable<ObjectRelationshipCount[]> {        
        return this.http.get(`/api/${objectType}/${objectId}/relationships/counts`)
            .pipe(
                map(response => <ObjectRelationshipCount[]>response),
                catchError(err=> this.handleError(err))
            );
        
    }

    getObjectRelationships(objectType: string, objectId: number, targetType: string, targetTypeId: number, intersectTypeID: number, includeInverse: boolean = true): Observable<any> {
        return this.http.get(`/api/${objectType}/${objectId}/relationships/${targetType}/${targetTypeId}/${intersectTypeID}?includeInverse=${includeInverse}`)
            .pipe(
                map(response => response),
                catchError(err=>this.handleError(err))
            );
    }

    getTechnicalRelationships(objectType: string, objectId: number): Observable<any> {        
        return this.http.get(`/api/${objectType}/${objectId}/relations`)
            .pipe(
                map(response => response),
                catchError(err=> this.handleError(err))
            );

    }

    exportObjectRelationshipsToExcel(objectType: string, objectId: number, targetType: string, targetTypeId: number, intersectTypeID: number, queryString: string, criticalOnly?: boolean){
        criticalOnly = (criticalOnly == undefined ? false : criticalOnly);

        window.location.assign(`/api/export/${objectType}/${objectId}/relationships/${targetType}/${targetTypeId}/${intersectTypeID}/excel.xls?${queryString}`);        
    }

    deleteRelationshipItem(id: number): Observable<any> {
        let url = `/api/relationships/${id}`;

        return this.http
            .delete(url)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    IsTransformPredicateExists(id: number): Observable<boolean> {
        return this.http.get(`api/v2/relationships/IsTransformPredicateExists/${id}`)
            .pipe(
                map(response =><boolean> response),
                catchError(err => this.handleError(err))
            );
    }
    
}