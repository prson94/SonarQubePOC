import { SelectItem } from '../models/form.model';
import { JsonResult } from './jsonresult.model';

export interface IGroupService {
    getGroupList(): Promise<GroupSearchResultModel[]>; 
    getGroupResourceList(id: number): Promise<GroupResourceInfo[]>; 
    getGroup(id: number): Promise<GroupEditorModel>; 
    putGroup(group: Group): Promise<JsonResult>; 
    postGroup(group: Group): Promise<JsonResult>;
    deleteGroup(id: number): Promise<JsonResult>;
    postResourceGroup(resourceGroup: ResourceGroup): Promise<JsonResult>;
    deleteResourceGroup(groupID: number, resourceID: number): Promise<JsonResult>;
    getGroupUserList(id: number): Promise<JsonResult>;
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


