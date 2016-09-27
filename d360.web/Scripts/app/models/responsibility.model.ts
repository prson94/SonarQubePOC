
import { SelectItem } from '../models/form.model';

export interface IResponsibilityService {
    getResponsibilityDetail(objectID: number, objectType: string, showHidden: boolean): Promise<ResponsibilityItem[]>;
    getResponsibilityItemEditor(objectID: number, objectType: string, responsibilityID: number): Promise<ResponsibilityEditorModel>;
    postResponsibility(responsibility: ResponsibilityItem): Promise<any>;
}

export class ResponsibilityEditorModel {
    resources: SelectItem[];
    //resourceList: SelectItem[];
    selectedResource: string;
    contexts: SelectItem[];
    //contextList: SelectItem[];
    selectedContexts: string[];
    responsibilityTypes: SelectItem[];
    //responsibilityTypeList: SelectItem[];
    selectedResponsibilityType: string;
    responsibility: ResponsibilityItem;
}

export class ResponsibilityItem {
    ID: number;
    ResponsibilityID: number;

    AssigningItemID: number;
    AssigningItemType: string;
    ContextItems: string;
    ObjectID: number;
    ObjectName: string;
    ObjectType: string;
    ObjectTypeID: number;
    ObjectTypeName: string;
    PrimaryOwnerResourceID: number;
    PrimaryOwnerResourceName: string;
    PrimaryOwnerResourceUrl: string;
    ResponsibilityTypeID: number;
    ResponsibleObjectID: number;
    ResponsibleObjectName: string;
    ResponsibleObjectType: string;
    ResponsibleObjectUrl: string;
    Role: string;
    Visible: boolean;
    ResponsibilityContextItems: ResponsibilityContextItem[]; 
}

export class ResponsibilityContextItem
{
    ResponsibiltyID: number; 
    ObjectType: string;
    ObjectID: number;
}