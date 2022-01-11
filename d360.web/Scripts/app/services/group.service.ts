import {Injectable} from '@angular/core';
import { HttpClient, HttpHeaders} from "@angular/common/http";
import {Observable} from "rxjs";
import {catchError, map} from "rxjs/operators";

import {
    IGroupService,
    GroupSearchResultModel,
    GroupResourceInfo,
    Group,
    AddUserToGroup
} from '../models/group.model';
import {JsonResult} from '../models/jsonresult.model';
import {CountObject} from '../models/resource.model';
import {MessagesObservableService} from './messages-observable.service';
import {BaseObservableService} from "./baseObservable.service";

@Injectable({
    providedIn: 'root'
})
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
            catchError((err) => this.handleError(err))
        );
    }

    getGroups(): Observable<any> {
        return this.http.get('api/v2/membership/groups')
            .pipe(
            map(x => <any>x),
                catchError(err=>this.handleError(err))
            );
    }

    deleteGroupWithUid(group: string): Observable<JsonResult> {
        const httpOptions = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' }),
            body: [{ Uid: group }]
        };

        return this.http.delete('api/v2/membership/groups', httpOptions).pipe(
            map((response) => <JsonResult>response),
            catchError((err) => this.handleError(err))
        );
    }

    getGroupResourceList(uid: string, pageSize: number): Observable<any> {
            return this.http.get(`api/v2/membership/groups/${uid}/members?_pageSize=${pageSize}`).pipe(
                map((response) => <GroupResourceInfo[]>response),
                catchError((err) => this.handleError(err))
            );
    }

    getGroupUid(id: number): Observable<any> {
        return this.http.get(`api/v2/membership/groups/${id}`).pipe(
            map((response) => <GroupResourceInfo[]>response),
            catchError((err) => this.handleError(err))
        );
    }

    getGroupMembers(groupUid: string): Observable<any> {
        return this.http.get(`api/v2/membership/groups/${groupUid}/members?_pageSize=250`).pipe(
            map((response) => <any[]>response),
            catchError((err) => this.handleError(err))
        );
    }


    getGroup(id: number,uid:string): Observable<any> {
        return this.http.get(`form/Group?id=${id}&uid=${uid}`).pipe(
            map((response) => <any>response),
            catchError((err) => this.handleError(err))
        );
    }

    putGroup(group: Group): Observable<any> {
        return this.http.put('api/v2/membership/groups', [group]).pipe(
            map((response) => <any>response),
            catchError((err) => this.handleError(err))
        );
    }

    postGroup(group: Group): Observable<any> {
        return this.http.post('api/v2/membership/groups', [group]).pipe(
            map((response) => <any>response),
            catchError((err) => this.handleError(err))
        );
    }

    addUsersToGroup(groupUid: string, users: AddUserToGroup[]): Observable<any> {
        return this.http.post(`api/v2/membership/groups/${groupUid}/members`, users).pipe(
            map((response) => <any>response),
            catchError((err) => this.handleError(err))
        );
    }

    deleteUsersFromGroup(groupUid: string, userUid: string): Observable<any> {
        return this.http.delete(`api/v2/membership/groups/${groupUid}/${userUid}`).pipe(
            map((response) => <any>response),
            catchError((err) => this.handleError(err))
        );
    }

    getResponsibilityBreakdownByGroup(id: number): Observable<CountObject[]> {
        return this.http.get(`/api/v2/social//ResponsibilityBreakdownByGroup?id=${id}`).pipe(
            map((response) => <CountObject[]>response),
            catchError((err) => this.handleError(err))
        );
    }
}
