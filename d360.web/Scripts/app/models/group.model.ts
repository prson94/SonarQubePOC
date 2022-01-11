import { SelectItem } from '../models/form.model';
import { JsonResult } from './jsonresult.model';
import { Observable } from "rxjs";

export interface IGroupService {
    getGroupList(): Observable<GroupSearchResultModel[]>;
    getGroupResourceList(id: string, pageSize: number): Observable<any>;
    getGroup(id: number, uid: string): Observable<GroupEditorModel>;
    putGroup(group: Group): Observable<JsonResult>;
    postGroup(group: Group): Observable<JsonResult>;
}


export class GroupSearchResultModel {
    ID: number;
    Name: string;
    NumberOfMembers: number;
    IsMember: boolean;
}

export class GroupApiModel {
    Uid: string;
    Name: string;
    PrimaryOwnerUid: string;
    SecondaryOwnerUid: string;
}

export class GroupApiModels {
    Items: GroupApiModel[];
    Total: Number;
}

export class GroupResourceInfo {
    GroupID: number;
    ResourceID: number;
    FirstName: string;
    LastName: string;
    Email: string;
    Owner: string;
    uid: string;
}

export class Group {
    Uid: string;
    ID: number;
    Name: string;
    Description: string;
    PrimaryOwnerResourceID: number;
    SecondaryOwnerResourceID: number;
    PrimaryOwnerUid: string;
    SecondaryOwnerUid: string;
    PrimaryOwnerName: string;
    SecondaryOwnerName: string;
    IsActiveDirectoryGroup: boolean;
    UpdatedOn: string;
    UpdatedBy: string;
}

export class ResourceGroup {
    GroupID: number;
    ResourceID: number;
    IsOwner: boolean;
}

export class ResourceGroupInfo {
    ResourceGroups: ResourceGroup[];
    GroupGuid: string;
}
export class GroupEditorModel {
    group: Group;
    resourceList: SelectItem[];
}

export class AddUserToGroup {
    Uid: string;
}


