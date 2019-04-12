import { SelectItem } from '../models/form.model';
import { JsonResult } from './jsonresult.model';
import {Observable} from "rxjs";

export interface IGroupService {
    getGroupList(): Observable<GroupSearchResultModel[]>;
    getGroupResourceList(id: number): Observable<GroupResourceInfo[]>;
    getGroup(id: number): Observable<GroupEditorModel>;
    putGroup(group: Group): Observable<JsonResult>;
    postGroup(group: Group): Observable<JsonResult>;
    deleteGroup(id: number): Observable<JsonResult>;
    postResourceGroup(resourceGroup: ResourceGroup[]): Observable<JsonResult>;
    deleteResourceGroup(groupID: number, resourceID: number): Observable<JsonResult>;
    getGroupUserList(id: number, pagenum: number, pagesize: number, sortDataField: string, sortOrder: string): Observable<JsonResult>;
}


export class GroupSearchResultModel {
    ID: number;
    Name: string;
    NumberOfMembers: number;
    IsMember: boolean;
}

export class GroupResourceInfo {
    GroupID: number;
    ResourceID: number;
    FirstName: string;
    LastName: string;
    Email: string;
    Owner: string;
}

export class Group {
    ID: number;    
    Name: string;
    Description: string;
    PrimaryOwnerResourceID: number;
    SecondaryOwnerResourceID: number;
    PrimaryOwnerName: string;
    SecondaryOwnerName: string;
    UpdatedOn: string;
    UpdatedBy: string;
}

export class ResourceGroup {
    GroupID: number;
    ResourceID: number;
    IsOwner: boolean;
}

export class GroupEditorModel {
    group: Group;
    resourceList: SelectItem[];
}


