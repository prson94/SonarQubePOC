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
        this.http.get(`api/v2/relationships/export/${relType.Uid}`, { responseType: 'blob' }).subscribe(data => this.downloadFile(data, 'Relationships'));
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

    saveRelationship(relationship: RelationshipDetail): Observable<JsonResult> {
        if (relationship.ID == undefined || !relationship.ID) {
            return this.postDynamic(this.http, 'intersecttype', relationship);
        }
        return this.putDynamic(this.http, 'intersecttype', relationship);
    }

    getRelationshipPredicates(subject: string, subjectId: number, object?: string, objectId?: number, predicateId?: number): Observable<PredicateDropdown[]> {
        let url = `form/IntersectType_PredicateOptions?subject=${subject}&subjectID=${subjectId}`;
        if (object != undefined)
            url = url += `&object=${object}`;
        if (objectId != undefined)
            url = url += `&objectID=${objectId}`;
        if (predicateId != undefined)
            url = url += `&predicateID=${predicateId}`;
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
                .subscribe((data) => this.downloadFile(data, 'relationship type items'));
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
}