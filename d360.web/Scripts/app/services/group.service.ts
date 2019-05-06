import {Injectable} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {Observable} from "rxjs";
import {catchError, map} from "rxjs/operators";

import {
    IGroupService,
    GroupSearchResultModel,
    GroupResourceInfo,
    Group,
    GroupEditorModel,
    ResourceGroup
} from '../models/group.model';
import {JsonResult} from '../models/jsonresult.model';
import {CountObject} from '../models/resource.model';

import {MessagesObservableService} from './messages-observable.service';
import {BaseObservableService} from "./baseObservable.service";

@Injectable()
export class GroupService extends BaseObservableService implements IGroupService {
    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) {
        super(messagesService);
    }

    getGroupList(): Observable<GroupSearchResultModel[]> {
        return this.http.get('api/groups').pipe(
            map(r => <GroupSearchResultModel[]>r),
            catchError(err => this.handleError(err))
        );
    }

    getGroupResourceList(id: number): Observable<GroupResourceInfo[]> {
        return this.http.get(`api/groups/${id}/resources`).pipe(
            map(response => <GroupResourceInfo[]>response),
            catchError(err => this.handleError(err))
        );
    }

    getGroup(id: number): Observable<GroupEditorModel> {
        return this.http.get(`form/Group?id=${id}`).pipe(
            map(response => <GroupEditorModel>response),
            catchError(err => this.handleError(err))
        );
    }

    putGroup(group: Group): Observable<JsonResult> {
        return this.http.put('form/Group', group).pipe(
            map(response => <JsonResult>response),
            catchError(err => this.handleError(err))
        );
    }

    postGroup(group: Group): Observable<JsonResult> {
        return this.http.post('form/Group', group).pipe(
            map(response => <JsonResult>response),
            catchError(err => this.handleError(err))
        );
    }

    deleteGroup(id: number): Observable<JsonResult> {
        return this.http.delete(`form/DeleteGroupByID?id=${id}`).pipe(
            map(response => <JsonResult>response),
            catchError(err => this.handleError(err))
        );
    }

    postResourceGroup(resourceGroups: ResourceGroup[]): Observable<JsonResult> {
        return this.http.post('form/ResourceGroup', resourceGroups).pipe(
            map(response => <JsonResult>response),
            catchError(err => this.handleError(err))
        );
    }

    deleteResourceGroup(
        groupID: number,
        resourceID: number
    ): Observable<JsonResult> {
        return this.http.delete(`form/ResourceGroup?groupID=${groupID}&resourceID=${resourceID}`).pipe(
            map(response => <JsonResult>response),
            catchError(err => this.handleError(err))
        );
    }

    getGroupUserList(
        id: number,
        pagenum: number,
        pagesize: number,
        sortDataField: string,
        sortOrder: string
    ): Observable<any> {
        return this.http.get(`form/GetGroupUserList?id=${id}&pagenum=${pagenum}&pagesize=${pagesize}&sortdatafield=${sortDataField}&sortorder=${sortOrder}`).pipe(
            map(response => response),
            catchError(err => this.handleError(err))
        );
    }

    getResponsibilityBreakdownByGroup(id: number): Observable<CountObject[]> {
        return this.http.get(`/api/v2/social//ResponsibilityBreakdownByGroup?id=${id}`).pipe(
            map(response => <CountObject[]>response),
            catchError(err => this.handleError(err))
        );
    }
}
