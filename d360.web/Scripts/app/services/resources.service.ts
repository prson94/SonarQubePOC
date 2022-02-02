import { catchError, map } from 'rxjs/operators';
import { Injectable } from '@angular/core';
import { HttpClient, HttpContext, HttpHeaders } from '@angular/common/http';
import { HelpResource, Resource, CountObject, ResponsibilityDetailForResource, FollowingDetailForResource, ResourceAPICredentials, MulitSelectResourceData, ResourceApiModel } from '../models/resource.model';
import { JsonResult } from '../models/jsonresult.model';
import { Observable, throwError } from "rxjs";
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';
import { ApiResult } from '../models/apiresult.model';
import { ROUTE_INDEPENDENT_QUERY } from '../http-interceptors';


@Injectable({
    providedIn: 'root'
})
export class ResourcesService extends BaseObservableService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    getHelpResources(): Observable<HelpResource[]> {
        return this.http.get(
            '/resources/HelpResources',
            { context: new HttpContext().set(ROUTE_INDEPENDENT_QUERY, true) }
        )
            .pipe(
                map((response) => <HelpResource[]>response),
                catchError((err) => this.handleError(err))
            );
    }

    getResources(includeInactive: boolean = true): Observable<Resource[]> {

        let url = '/api/resources/1';
        if (includeInactive === false) {
            url += '?includeInactive=false';
        }
        return this.http.get(url)
            .pipe(
                map((response) => <Resource[]>response),
                catchError((err) => this.handleError(err))
            );

    }

    getResource(id: number): Observable<any> {
        return this.http.get(`/api/v2/membership/users?ResourceID=${id}`).pipe(
            map((response) => {
                return <any>response;
            }),
            catchError((err) => {
                if (this.isErrorFromFilterExpression(err)) {
                    return throwError(err);
                }
                return this.handleError(err);
            }));
    }

    public saveResource(
        resource: ResourceApiModel,
        lookupFieldsPassedByValue: boolean = true,
        IsChangePasswordReqeust: boolean = false
    ): Observable<ApiResult> {

        const httpOptions = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' })
        };
        const resourceArray: ResourceApiModel[] = [];
        resourceArray.push(resource);

        if (resource.uid) {

            return this
                .http
                .put(`api/v2/membership/users?lookupFieldsPassedByValue=${lookupFieldsPassedByValue}&IsChangePasswordReqeust=${IsChangePasswordReqeust}`, resourceArray, httpOptions)
                .pipe(
                    map((res: ApiResult) => {
                        return res[0];
                    }),
                    catchError((err) => this.handleError(err))
                );
        }
        else {
            return this
                .http
                .post(`api/v2/membership/users?lookupFieldsPassedByValue=${lookupFieldsPassedByValue}`, resourceArray, httpOptions)
                .pipe(
                    map((res: ApiResult[]) => {
                        return res[0];
                    }),
                    catchError((err) => this.handleError(err))
                );
        }
    }

    public deleteResource(uid: string): Observable<JsonResult> {
        var model = [];
        model.push({ Uid: uid });
        const httpOptions = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' }),
            body: model
        };

        return this
            .http
            .delete(`api/v2/membership/users`, httpOptions)
            .pipe(
                map(res => <JsonResult>res),
                catchError((err) => this.handleError(err))
            );
    }

    getResourceLazy(params: any): Observable<any> {

        var qString = '';
        if (params) {
            qString = Object.keys(params).map(key => key + '=' + params[key]).join('&');
            if (qString)
                qString = '?' + qString;
        }

        return this.http.get('/api/v2/membership/users' + qString).pipe(
            map((response) => {
                return response;
            }),
            catchError((err) => {
                if (this.isErrorFromFilterExpression(err)) {
                    return throwError(err);
                }
                return this.handleError(err);
            }));
    }

    exportResources(params: any, filename: string): Observable<any> {
        params['_pageNum'] = 1;
        params['_pageSize'] = 100000;

        var qString = '';
        if (params) {
            qString = Object.keys(params).map(key => key + '=' + params[key]).join('&');
            if (qString)
                qString = '?' + qString;
        }

        return this.http.get('/api/v2/membership/users' + qString,
            { headers: new HttpHeaders({ 'Accept': 'application/octet-stream' }), responseType: 'blob' })
            .pipe(map((data) => this.downloadFile(data, filename)),
                catchError((err) => this.handleError(err))
            );
    }

    getResponsibilityBreakdownByResource(id: number, responsibilityTypeUid: string = ""): Observable<CountObject[]> {
        var url = "";
        if (responsibilityTypeUid !== "") {
            url = `/api/v2/social/ResponsibilityBreakdownByResource?id=${id}&responsibilityTypeUID=${responsibilityTypeUid}`;
        }
        else {
            url = `/api/v2/social/ResponsibilityBreakdownByResource?id=${id}`;
        }

        return this.http.get(url)
            .pipe(
                map((response) => <CountObject[]>response),
                catchError((err) => this.handleError(err))
            );

    }

    getFollowingBreakdownByResource(id: number): Observable<CountObject[]> {
        return this.http.get(`/api/v2/social/FollowingBreakdownByResource?id=${id}`)
            .pipe(
                map((response) => <CountObject[]>response),
                catchError((err) => this.handleError(err))
            );

    }

    getResponsibilitiesByResourceByType(type: string, id: number, targetType: string, targetId: number, responsibilityTypeId: number = null): Observable<ResponsibilityDetailForResource[]> {
        let uri = `api/${type}/${id}/ownership/${targetType}/${targetId}`;
        if (responsibilityTypeId != null && responsibilityTypeId > 0)
            uri += `?responsibilityTypeId=${responsibilityTypeId}`;
        return this.http.get(uri)
            .pipe(
                map((response) => <ResponsibilityDetailForResource[]>response),
                catchError((err) => this.handleError(err))
            );
    }

    //    public JsonNetResult GetFollowingByResourceByType(int resourceID, string type, int id)
    getFollowingByResourceByType(resourceID: number, type: string, id: number): Observable<FollowingDetailForResource[]> {
        return this.http.get(`queries/followingbyresourcebytype?resourceID=${resourceID}&type=${type}&id=${id}`)
            .pipe(
                map((response) => <FollowingDetailForResource[]>response),
                catchError((err) => this.handleError(err))
            );
    }

    exportFollowingByResourceByType(resourceID: number, type: string, id: number) {
        window.location.assign(`/resources/${resourceID}/following/${type}/${id}.xlsx`);
    }

    exportResponsibilitiesByResourceByType(resourceID: number, type: string, id: number, responsibilityTypeUid: string = null) {
        let uri = `/resources/${resourceID}/ownership/${type}/${id}.xlsx`
        if (responsibilityTypeUid != null && responsibilityTypeUid !== "") {
            uri += `?responsibilityTypeId=${responsibilityTypeUid}`;
        }
        window.location.assign(uri);
    }

    getApiKeys(): Observable<ResourceAPICredentials> {
        return this.http
            .get(
                '/api/v2/membership/users/me/apikey',
                { context: new HttpContext().set(ROUTE_INDEPENDENT_QUERY, true) }
            )
            .pipe(
                map((response) => <ResourceAPICredentials>response),
                catchError((err) => this.handleError(err))
            );
    }

    regenerateApiKeys(model: ResourceAPICredentials): Observable<ResourceAPICredentials> {
        const httpOptions = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' })
        };
        return this.http.post('/api/v2/membership/users/me/apikey', model, httpOptions)
            .pipe(
                map((response) => <ResourceAPICredentials>response),
                catchError((err) => this.handleError(err))
            );
    }

    getUserGroups(resourceUid: string): Observable<any> {
        return this.http.get(`/api/v2/membership/groups?ResourceUid=${resourceUid}`)
            .pipe(
                map((response) => response),
                catchError((err) => this.handleError(err))
            );
    }

    resetResourcesPassword(resourceID: number): Observable<JsonResult> {
        let headers = new HttpHeaders({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8' //pass as text since its a dynamic object and mvc has issue with dynamic models
        });

        return this.http
            .post(`form/ResetResourcePassword`, 'ID=' + resourceID, { headers: headers })
            .pipe(
                map((response) => response),
                catchError((err) => this.handleError(err))
            );
    }

    getResourceItems(uri: string): Observable<MulitSelectResourceData> {
        return this.http.get(uri)
            .pipe(
                map((response) => <MulitSelectResourceData>response),
                catchError((err) => this.handleError(err))
            );
    }

    getLegacyData(uid: string): Observable<any> {
        return this.http.get(`/api/v2/membership/legacyData/resource/${uid}`)
            .pipe(map(res => <any>res),
                catchError((err) => this.handleError(err)));
    }
}