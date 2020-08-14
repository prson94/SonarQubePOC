import { catchError, map } from 'rxjs/operators';
import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { HelpResource, Resource, CountObject, ResponsibilityDetailForResource, FollowingDetailForResource, ResourceAPICredentials, MulitSelectResourceData } from '../models/resource.model';
import { JsonResult } from '../models/jsonresult.model';
import { Observable, throwError } from "rxjs";
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';


@Injectable()
export class ResourcesService extends BaseObservableService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    getHelpResources(): Observable<HelpResource[]> {
        return this.http.get('/resources/HelpResources')
            .pipe(
                map(response => <HelpResource[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getResources(): Observable<Resource[]> {
        return this.http.get('/api/resources/1')
            .pipe(
                map(response => <Resource[]>response),
                catchError(err => this.handleError(err))
            );

    }

    getResource(id: number): Observable<Resource> {
        return this.http.get(`/api/resources/1/${id}`)
            .pipe(
                map(response => <Resource>response),
                catchError(err => this.handleError(err))
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
            map(response => {
                return response;
            }),
            catchError(err => {
                if (this.isErrorFromFilterExpression(err)) {
                    return throwError(err);
                }
                return this.handleError(err);
            }));
    }

    exportResources(params: any) {
        params['_pageNum'] = 1;
        params['_pageSize'] = 100000;

        var qString = '';
        if (params) {
            qString = Object.keys(params).map(key => key + '=' + params[key]).join('&');
            if (qString)
                qString = '?' + qString;
        }

        this.http.get('/api/v2/membership/users' + qString,
            { headers: new HttpHeaders({ 'Accept': 'application/octet-stream' }), responseType: 'blob' })
            .subscribe((data: any) => this.downloadFile(data, "Users.xlsx"));
    }

    getResponsibilityBreakdownByResource(id: number, responsibilityTypeId: number = 0): Observable<CountObject[]> {
        var url = "";
        if (responsibilityTypeId > 0) {
            url = `/api/v2/social/ResponsibilityBreakdownByResource?id=${id}&responsibilityTypeID=${responsibilityTypeId}`;
        }
        else {
            url = `/api/v2/social/ResponsibilityBreakdownByResource?id=${id}`;
        }

        return this.http.get(url)
            .pipe(
                map(response => <CountObject[]>response),
                catchError(err => this.handleError(err))
            );

    }

    getFollowingBreakdownByResource(id: number): Observable<CountObject[]> {
        return this.http.get(`/api/v2/social/FollowingBreakdownByResource?id=${id}`)
            .pipe(
                map(response => <CountObject[]>response),
                catchError(err => this.handleError(err))
            );

    }

    getResponsibilitiesByResourceByType(type: string, id: number, targetType: string, targetId: number, responsibilityTypeId: number = null): Observable<ResponsibilityDetailForResource[]> {
        let uri = `api/${type}/${id}/ownership/${targetType}/${targetId}`;
        if (responsibilityTypeId != null && responsibilityTypeId > 0)
            uri += `?responsibilityTypeId=${responsibilityTypeId}`;
        return this.http.get(uri)
            .pipe(
                map(response => <ResponsibilityDetailForResource[]>response),
                catchError(err => this.handleError(err))
            );
    }

    //    public JsonNetResult GetFollowingByResourceByType(int resourceID, string type, int id)
    getFollowingByResourceByType(resourceID: number, type: string, id: number): Observable<FollowingDetailForResource[]> {
        return this.http.get(`queries/followingbyresourcebytype?resourceID=${resourceID}&type=${type}&id=${id}`)
            .pipe(
                map(response => <FollowingDetailForResource[]>response),
                catchError(err => this.handleError(err))
            );
    }

    exportFollowingByResourceByType(resourceID: number, type: string, id: number) {
        window.location.assign(`/resources/${resourceID}/following/${type}/${id}.xlsx`);
    }

    exportResponsibilitiesByResourceByType(resourceID: number, type: string, id: number, responsibilityTypeId: number = null) {
        let uri = `/resources/${resourceID}/ownership/${type}/${id}.xlsx`
        if (responsibilityTypeId != null && responsibilityTypeId > 0)
            uri += `?responsibilityTypeId=${responsibilityTypeId}`;
        window.location.assign(uri);
    }

    getMyCredentials(): Observable<ResourceAPICredentials> {
        return this.http.get('resources/myapicredentials')
            .pipe(
                map(response => <ResourceAPICredentials>response),
                catchError(err => this.handleError(err))
            );
    }

    getUserGroups(resourceID: number): Observable<any[]> {
        return this.http.get(`resources/_GroupsByResourceID?id=${resourceID}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    resetResourcesPassword(resourceID: number): Observable<JsonResult> {
        let headers = new HttpHeaders({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8' //pass as text since its a dynamic object and mvc has issue with dynamic models
        });

        return this.http
            .post(`form/ResetResourcePassword`, 'ID=' + resourceID, { headers: headers })
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getResourceItems(uri: string): Observable<MulitSelectResourceData> {
        return this.http.get(uri)
            .pipe(
                map(response => <MulitSelectResourceData>response),
                catchError(err => this.handleError(err))
            );
    }

    downloadFile(data: Blob, filename: string) {
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

    getLegacyData(uid: string): Observable<any> {
        return this.http.get(`/api/v2/membership/legacyData/resource/${uid}`)
            .pipe(map(res => <any>res),
                catchError((err) => this.handleError(err)));
    }
}