///<reference path="../es6-shim.d.ts"/>

export class ResponsibilityItem {
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