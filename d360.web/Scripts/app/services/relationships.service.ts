import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map, publishReplay, refCount } from 'rxjs/operators';
import { MessagesObservableService } from './messages-observable.service';
import { BaseObservableService } from './baseObservable.service';
import { RelationshipType, RelationshipDetail, ObjectRelationship, RelatedItem, ObjectRelationshipCount, PredicateDropdown, RelationshipCount, RelationItem } from '../models/relationship.model';
import { JsonResult } from '../models/jsonresult.model';
import { DropdownOption } from '../models/dropdown.model';
import { Observable, forkJoin } from 'rxjs';
import { ApiResult } from '../models/apiresult.model';
import { Relation } from '../models/fieldtype-api.model';

@Injectable({
    providedIn: 'root'
})
export class RelationshipsService extends BaseObservableService {

    private MAX_SYNCHRONOUS_API_ITEM_COUNT: number = 250;

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    getRelationshipTypes(assetTypeUid: string = null): Observable<RelationshipType[]> {
        var url = 'api/v2/relationships/types?state=1';

        if (assetTypeUid) {
            url += `&AssetTypeUid=${assetTypeUid}`;
        }

        return this.http.get(url)
            .pipe(
                map(response => <RelationshipType[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getRelationshipTypesByAssetUid(uid: string): Observable<RelationshipType[]> {
        return this.http.get(`api/v2/relationships/types?state=1&AssetTypeUid=${uid}`)
            .pipe(
                map(response => <RelationshipType[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getRelationshipTypesById(id: number, type: string): Observable<RelationshipType[]> {
        return this.http.get(`api/v2/relationships/types/${id}/${type}`)
            .pipe(
                map(response => <RelationshipType[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getRelationshipTypesForComplexField(assetUid: string, fieldName: string): Observable<RelationshipType[]> {
        return this.http.get(`api/v2/relationships/types/complexField/${assetUid}/${fieldName}`)
            .pipe(
                map(response => <RelationshipType[]>response),
                catchError(err => this.handleError(err))
            );
    }

    saveRelationshipType(relationshipType: RelationshipType): Observable<any> {
        if (relationshipType) {
            return this.http.request(relationshipType.Uid == null ? 'POST' : 'PUT', 'api/v2/relationships/types', {
                body: [{
                    Uid: relationshipType.Uid,
                    PredicateUid: relationshipType.Predicate.Uid,
                    SubjectUid: relationshipType.Subject.Uid,
                    SubjectCardinality: relationshipType.Subject.Cardinality,
                    ObjectUid: relationshipType.Object.Uid,
                    ObjectCardinality: relationshipType.Object.Cardinality
                }]
            })
                .pipe(
                    map(response => <any>response),
                    catchError(err => this.handleError(err))
                );
        }
    }

    saveRelationships(intersectTypeUid: string, model: any[]): Observable<ApiResult[]> {
        if (model.length > this.MAX_SYNCHRONOUS_API_ITEM_COUNT) {
            var models: any[] = [];
            for (var i = 0; i < model.length; i += this.MAX_SYNCHRONOUS_API_ITEM_COUNT) {
                models.push(model.slice(i, i + this.MAX_SYNCHRONOUS_API_ITEM_COUNT));
            }
            var obsArr: Observable<ApiResult[]>[] = [];
            models.forEach(m => {
                obsArr.push(this.saveRelationships(intersectTypeUid, m));
            });
            return forkJoin(obsArr).pipe(
                map(response => {
                    var origResponse: ApiResult[] = [];
                    response.forEach(res => {
                        res.forEach(r =>
                            origResponse.push(r))
                    });
                    for (let i = 0; i < response.length; i++) {
                        origResponse[i].ItemNumber = i + 1;
                    }
                    return origResponse;
                }),
                catchError(err => this.handleError(err, true))
            );
        }
        else {
            return this.http.post(`api/v2/relationships/${intersectTypeUid}/?triggerWorkflow=true&lookupFieldsPassedByValue=true`, model).pipe(
                map(response => response),
                catchError(err => this.handleError(err, true))
            );
        }
    }

    saveRelationshipsForked(intersectTypeUid: number, model: any[]): Observable<any> {
        var obj = { intersectTypeUid, model };
        return this.http.post(`api/v2/relationships/${intersectTypeUid}/?triggerWorkflow=true&lookupFieldsPassedByValue=true`, model).pipe(
            map(response => { return { obj, response }; }),
            catchError(err => this.handleError(err, true))
        );
    }

    deleteSingleRelationshipV2(intersectTypeUid: string, intersectUid: string): Observable<ApiResult[]> {
        const model = [{
            Cascade: true,
            Uid: intersectUid
        }];

        const httpHeaders = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' }), body: model
        };
        return this.http.delete(`api/v2/relationships/${intersectTypeUid}/?triggerWorkflow=true`, httpHeaders).pipe(
            map(response => response),
            catchError(err => this.handleError(err, true))
        );
    }

    deleteRelationshipV2(intersectTypeUid: string, model: any[]): Observable<ApiResult[]> {
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

    getRelation(id: number): Observable<RelationshipDetail> {
        return this.http.get(`form/IntersectType_FormData?id=${id}`)
            .pipe(
                map(response => <RelationshipDetail>response),
                catchError(err => this.handleError(err))
            );
    }

    getIntersectTypeById(id: number): Observable<any> {
        return this.http.get(`api/v2/relationships/types/${id}`)
            .pipe(
                map(response => <any>response),
                catchError(err => this.handleError(err))
            );
    }

    getRelationshipUids(relationshipTypeUid: string): Observable<any> {
        return this.http.get(`api/v2/relationships/uids/${relationshipTypeUid}`)
            .pipe(
                map(response => <any>response),
                catchError(err => this.handleError(err))
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
                catchError(err => this.handleError(err))
            );
    }

    deleteRelationship(id: number): Observable<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'intersecttype', id);
    }

    deleteRelationshipType(uid: string): Observable<any> {
        return this.http.delete('api/v2/relationships/types', { body: [{ uid }] })
            .pipe(
                map(response => <any>response),
                catchError(err => this.handleError(err))
            );
    }

    saveRelationship(relationship: RelationshipDetail): Observable<JsonResult> {
        if (relationship.ID == undefined || !relationship.ID) {
            return this.postDynamic(this.http, 'intersecttype', relationship);
        }
        return this.putDynamic(this.http, 'intersecttype', relationship);
    }

    getRelationshipPredicates(subjectUid: string, objectUid?: string, predicateUid?: string): Observable<PredicateDropdown[]> {
        let url = `form/IntersectType_PredicateOptions?subjectUid=${subjectUid}`;
        if (objectUid != undefined) {
            url = url += `&objectUid=${objectUid}`;
        }
        if (predicateUid != undefined) {
            url = url += `&predicateUid=${predicateUid}`;
        }
        return this.http.get(url)
            .pipe(
                map(response => <PredicateDropdown[]>response),
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
                catchError(err => this.handleError(err))
            );
    }

    getObjectOptions(subjectUid: string, objectUid?: string, predicateUid?: string): Observable<DropdownOption[]> {
        let url = `form/IntersectType_ObjectOptions?subjectUid=${subjectUid}`;
        if (objectUid != undefined) {
            url = url += `&objectUid=${objectUid}`;
        }
        if (predicateUid != undefined) {
            url = url += `&predicateUid=${predicateUid}`;
        }

        return this.http.get(url)
            .pipe(
                map(response => <DropdownOption[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getRelationshipCounts(objectType: string, objectId: number): Observable<ObjectRelationshipCount[]> {
        return this.http.get(`/api/${objectType}/${objectId}/relationships/counts`)
            .pipe(
                map(response => <ObjectRelationshipCount[]>response),
                catchError(err => this.handleError(err))
            );

    }

    getObjectRelationships(objectType: string, objectId: number, targetType: string, targetTypeId: number, intersectTypeID: number, includeInverse: boolean = true, sourceIsObject: boolean = false): Observable<any> {
        return this.http.get(`/api/${objectType}/${objectId}/relationships/${targetType}/${targetTypeId}/${intersectTypeID}?includeInverse=${includeInverse}&sourceIsObject=${sourceIsObject}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    public getRelations(
        object: string,
        objectId: number
    ): Observable<RelationItem[]> {
        return this
            .http
            .get(`api/${object}/${objectId}/relations`)
            .pipe(
                map((response) => <RelationItem[]>response),
                catchError(err => this.handleError(err))
            );
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
                map(response => <boolean>response),
                catchError(err => this.handleError(err))
            );
    }


    private tagTooltipsCache: any[] = [];

    getRelationshipsByAssetTypeUid(assetTypeUid: string): Observable<RelationshipType[]> {

        var cachedItem = this.tagTooltipsCache.find(x => x.assetTypeUid == assetTypeUid);
        if (cachedItem)
            return cachedItem.obs;

        let url = `api/v2/relationships/types?AssetTypeUid=${assetTypeUid}&State=Active`;

        var obs = this.http.get(url)
            .pipe(map(response => <RelationshipType[]>response),
                publishReplay(1),
                refCount(),
                catchError(err => this.handleError(err)));

        var data = { assetTypeUid: assetTypeUid, obs: obs };
        this.tagTooltipsCache.push(data);

        return obs;
    }

    getRelationships(intersectTypeUid: string, params: any, isExport = false): Observable<any> {
        var url = 'api/v2/relationships?RelationshipTypeUid=' + intersectTypeUid + '&_includepath=true';

        if (params) {
            url += "&" + Object.keys(params).map((key) => key + '=' + params[key]).join('&');
        }

        if (isExport === false) {
            return this.http.get(url)
                .pipe(
                    map((response) => <any>response),
                    catchError((err) => {
                        if (!this.isErrorFromFilterExpression(err)) {
                            return this.handleError(err);
                        }
                        else {
                            throw err;
                        }

                    }
                    )
                );
        }
        else {
            this.http.get(url, { headers: new HttpHeaders({ 'Accept': 'application/octet-stream' }), responseType: 'blob' })
                .subscribe((data) => this.downloadFile(data, 'Relationships'));
        }
    }

    getRelationshipsCountsForAsset(assetUid: string): Observable<RelationshipCount[]> {
        var url = 'api/v2/relationships/counts/' + assetUid;

        return this.http.get(url)
            .pipe(
                map(response => <any>response),
                catchError(err => this.handleError(err))
            );
    }

    getRelationshipsForAsset(assetUid: string, params: any): Observable<any[]> {
        var url = `/api/v2/relationships?AssetUid=${assetUid}`
        if (!params) {
            params = {};
        }
        params["State"] = "Active";
        params["_includeTotal"] = "true";
        params["_includePath"] = "true";

        if (params) {
            url += "&" + Object.keys(params).map((key) => key + '=' + params[key]).join('&');
        }

        return this.http.get(url)
            .pipe(
                map(response => <any>response),
                catchError(err => this.handleError(err))
            );
    }
}