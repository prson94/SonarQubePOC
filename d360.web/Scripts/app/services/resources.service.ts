
import {catchError, map} from 'rxjs/operators';
import { Injectable } from '@angular/core';
import { Headers, Http, ResponseContentType } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { HelpResource, Resource, CountObject, ResponsibilityDetailForResource, FollowingDetailForResource, ResourceAPICredentials, MulitSelectResourceData } from '../models/resource.model';
import { JsonResult } from '../models/jsonresult.model';
import { SortOrder } from '../models/enums.model';
import { GridFilterExpression } from '../models/grid-definition.model';
import { Observable } from "rxjs";


@Injectable()
export class ResourcesService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getHelpResources(): Promise<HelpResource[]> {
        return this.http.get('/resources/HelpResources')
            .toPromise()
            .then(response => <HelpResource[]>response.json())
            .catch(err => this.handleError(err));
    }

    getResources(): Promise<Resource[]> {
        return this.http.get('/api/resources/1')
            .toPromise()
            .then(response => <Resource[]>response.json())
            .catch(err => this.handleError(err));
    }

    getResource(id: number): Promise<Resource> {
        return this.http.get(`/api/resources/1/${id}`)
            .toPromise()
            .then(response => <Resource>response.json())
            .catch(err => this.handleError(err));
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
                return response.json()
            }),
            catchError(err => this.handleError(err)),);
    }

    getResponsibilityBreakdownByResource(id: number, responsibilityTypeId: number = 0): Promise<CountObject[]> {
        var url = "";
        if (responsibilityTypeId > 0) {
            url = `/api/v2/social/ResponsibilityBreakdownByResource?id=${id}&responsibilityTypeID=${responsibilityTypeId}`;
        }
        else {
            url = `/api/v2/social/ResponsibilityBreakdownByResource?id=${id}`;
        }

        return this.http.get(url)
            .toPromise()
            .then(response => <CountObject[]>response.json())
            .catch(err => this.handleError(err));
    }

    getFollowingBreakdownByResource(id: number): Promise<CountObject[]> {
        return this.http.get(`/api/v2/social/FollowingBreakdownByResource?id=${id}`)
            .toPromise()
            .then(response => <CountObject[]>response.json())
            .catch(err => this.handleError(err));
    }

    getResponsibilitiesByResourceByType(type: string, id: number, targetType: string, targetId: number, responsibilityTypeId: number = null): Promise<ResponsibilityDetailForResource[]> {
        let uri = `api/${type}/${id}/ownership/${targetType}/${targetId}`;
        if (responsibilityTypeId != null && responsibilityTypeId > 0)
            uri += `?responsibilityTypeId=${responsibilityTypeId}`;
        return this.http.get(uri)
            .toPromise()
            .then(response => <ResponsibilityDetailForResource[]>response.json())
            .catch(err => this.handleError(err));
    }

    //    public JsonNetResult GetFollowingByResourceByType(int resourceID, string type, int id)
    getFollowingByResourceByType(resourceID: number, type: string, id: number): Promise<FollowingDetailForResource[]> {
        return this.http.get(`queries/followingbyresourcebytype?resourceID=${resourceID}&type=${type}&id=${id}`)
            .toPromise()
            .then(response => <FollowingDetailForResource[]>response.json())
            .catch(err => this.handleError(err));
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

    getMyCredentials(): Promise<ResourceAPICredentials> {
        return this.http.get('overlays/myapicredentialsng')
            .toPromise()
            .then(response => <ResourceAPICredentials>response.json())
            .catch(err => this.handleError(err));
    }

    getUserGroups(resourceID: number): Promise<any[]> {
        return this.http.get(`resources/_GroupsByResourceID?id=${resourceID}`)
            .toPromise()
            .then(response => <any[]>response.json())
            .catch(err => this.handleError(err));
    }
    
    resetResourcesPassword(resourceID: number): Promise<JsonResult> {
        let headers = new Headers({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8' //pass as text since its a dynamic object and mvc has issue with dynamic models
        });

        return this.http
            .post(`form/ResetResourcePassword`, 'ID=' + resourceID, { headers: headers })
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(err => this.handleError(err));
    }

    getResourceItems(uri: string): Promise<MulitSelectResourceData> {
        return this.http.get(uri)
            .toPromise()
            .then(response => <MulitSelectResourceData>response.json())
            .catch(err => this.handleError(err));
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

        this.http.get(url, { responseType: ResponseContentType.Blob }).subscribe((data: any) => this.downloadFile(data, "Users.xlsx"));  
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