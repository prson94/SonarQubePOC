
import {catchError, map} from 'rxjs/operators';
import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { HelpResource, Resource, CountObject, ResponsibilityDetailForResource, FollowingDetailForResource, ResourceAPICredentials, MulitSelectResourceData } from '../models/resource.model';
import { JsonResult } from '../models/jsonresult.model';
import { SortOrder } from '../models/enums.model';
import { GridFilterExpression } from '../models/grid-definition.model';
import { Observable } from "rxjs";
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';


@Injectable()
export class ResourcesService extends BaseObservableService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    getHelpResources(): Observable<HelpResource[]> {
        return this.http.get('/resources/HelpResources')
            .pipe(
                map(response => <HelpResource[]>response),
                catchError(err=>this.handleError(err))
            );
    }

    getResources(): Observable<Resource[]> {
        return this.http.get('/api/resources/1')
            .pipe(
                map(response => <Resource[]>response),
                catchError(err=>this.handleError(err))
            );

    }

    getResource(id: number): Observable<Resource> {
        return this.http.get(`/api/resources/1/${id}`)
            .pipe(
                map(response => <Resource>response),
                catchError(err=> this.handleError(err))
            );

    }


    getResourceLazy(typeId: number, pageNum: number, pageSize: number, sortOrder: SortOrder, sortField?: string, simpleFilter?:string, filters?: GridFilterExpression[]): Observable<any> {
        let sortCol = sortField != undefined ? sortField : "";

        let url = `/resources/${typeId}/lazy?pagenum=${pageNum}&pagesize=${pageSize}&sortdatafield=${sortField}&sortorder=${sortOrder == SortOrder.None ? "" : (sortOrder == SortOrder.Ascending ? "asc" : "desc")}&simpleFilter=${simpleFilter}`;
        let indx = 0;

        if (filters != undefined) {
            url += `&filterscount=${filters.length}`;

            for (let filter of filters) {
                url += `&filtervalue${indx}=${filter.value}&filtercondition${indx}=${filter.condition}&filteroperator${indx}=1&filterdatafield${indx}=${filter.field}`;
                indx++;
            }
        }


        return this.http.get(url).pipe(
            map(response => {
               response
            }),
            catchError(err => this.handleError(err)),);
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
               catchError(err=>this.handleError(err))
            );

    }

    getFollowingBreakdownByResource(id: number): Observable<CountObject[]> {
        return this.http.get(`/api/v2/social/FollowingBreakdownByResource?id=${id}`)
            .pipe(
            map(response => <CountObject[]> response),
                catchError(err=>this.handleError(err))
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
                catchError(err=> this.handleError(err))
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
                catchError(err=> this.handleError(err))
            );
    }

    getUserGroups(resourceID: number): Observable<any[]> {
        return this.http.get(`resources/_GroupsByResourceID?id=${resourceID}`)
            .pipe(
                map(response => response),
                catchError(err=> this.handleError(err))
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
                catchError(err=> this.handleError(err))
            );
    }

    getResourceItems(uri: string): Observable<MulitSelectResourceData> {
        return this.http.get(uri)
            .pipe(
                map(response => <MulitSelectResourceData>response),
                catchError(err=> this.handleError(err))
            );
    }

    exportResources(typeId: number, sortOrder: SortOrder, sortField?: string, simpleFilter?: string, filters?: GridFilterExpression[]) {

        let sortCol = sortField != undefined ? sortField : "";

        let url = `/resources/${typeId}/lazy/excel?sortdatafield=${sortField}&sortorder=${sortOrder == SortOrder.None ? "" : (sortOrder == SortOrder.Ascending ? "asc" : "desc")}&simpleFilter=${simpleFilter}`;
        let indx = 0;

        if (filters != undefined) {
            url += `&filterscount=${filters.length}`;

            for (let filter of filters) {
                url += `&filtervalue${indx}=${filter.value}&filtercondition${indx}=${filter.condition}&filteroperator${indx}=1&filterdatafield${indx}=${filter.field}`;
                indx++;
            }
        }

        this.http.get(url, { responseType: 'blob' }).subscribe((data: any) => this.downloadFile(data, "Users.xlsx"));  
    }

    downloadFile(data: Response, filename: string) {
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
}