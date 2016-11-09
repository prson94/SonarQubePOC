import { SelectItem } from 'primeng/primeng'; 

export interface IResponsibilityTypeService {
    getResponsibilityTypes(): Promise<ResponsibilityType[]>;
    getResponsibilityType(id: number): Promise<ResponsibilityType>;
    putResponsibilityType(responsibilityType: ResponsibilityType): Promise<any>;
    postResponsibilityType(responsibilityType: ResponsibilityType): Promise<any>;
    deleteResponsibilityType(id: number): Promise<any>;
}

export class ResponsibilityType {
    ID: number;
    Name: string;
    ResponsbilityTypeGroup: ResponsibilityTypeGroup;
    Description: string;
    UpdatedOn: string;
    UpdatedBy: number;
    ResponsibilityTypeRelations: ResponsibilityTypeRelation[] = [];
    AllocationsList: SelectItem[] = [];
}


export class ResponsibilityTypeRelation {
    ResponsibilityTypeID: number;
    ObjectType: string;
    ObjectID: number;
}

export enum ResponsibilityTypeGroup {
    People = 1,
    Sourcing = 2
}

export class ResponsibilityTypeCount {
    Count: number;
    ResponsibilityType: string;
    ResponsibilityTypeID: number;
}


export class ResourceResponsibilityTypeCount {
    FirstName: string;
    LastName: string;
    OwnedItemCount: number;
    ResourceID: number;
    ResponsibilityType: string;
    ResponsibilityTypeID: number;
}