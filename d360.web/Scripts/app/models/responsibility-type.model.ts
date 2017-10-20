import { SelectItem } from 'primeng/primeng'; 

export interface IResponsibilityTypeService {
    getResponsibilityTypes(): Promise<ResponsibilityType[]>;
    getResponsibilityType(id: number): Promise<ResponsibilityType>;
    putResponsibilityType(responsibilityType: ResponsibilityType): Promise<any>;
    postResponsibilityType(responsibilityType: ResponsibilityType): Promise<any>;
    deleteResponsibilityType(id: number): Promise<any>;
    getResponsibilityTypeBreakdown(): Promise<ResponsibilityTypeCount[]>;
    getResourceResponsibilityByType(responsibilityTypeId: number): Promise<ResourceResponsibilityTypeCount[]>;
    getResponsibilityTypesByObject(type: string, id: number): Promise<any>;
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

export class ResponsibilityTypeRelationRuleSummary {
    ID: number;
    ResponsibilityTypeID: number;
    ResponsibilityType: string;
    Name: string;
}

export class ResponsibilityTypeRelationRule {
    ID: number;
    ResponsibilityTypeID: number;
    Name: string;
    Object: string;
    ObjectID: number;
    ObjectString: string;
    Definition: ResponsibilityTypeRelationRuleDefinition;
}

export class ResponsibilityTypeRelationRuleDefinition {
    When: ResponsibilityTypeRelationRuleDefinitionWhenItem[] = [];
    Then: ResponsibilityTypeRelationRuleDefinitionThen;
}

export class ResponsibilityTypeRelationRuleDefinitionWhenItem {
    CheckType: string;
    IntersectTypeID: number;
    TargetObject: string;
    TargetObjectID: number;
    FieldTypeID: number;
    FieldTypeName: string;
    Value: string;
    ValueOptions: SelectItem[] = [];
    IsLookup: boolean = false;
}

export class ResponsibilityTypeRelationRuleDefinitionWhenTestRow {
    Name: string;
}

export class ResponsibilityTypeRelationRuleDefinitionThenTestRow {
    ResourceName: string;
    GroupName: string;
}

export class ResponsibilityTypeRelationRuleDefinitionThen {
    Object: string;
    ObjectID: number;
    Conditions: ResponsibilityTypeRelationRuleDefinitionThenItem[] = [];
}

export class ResponsibilityTypeRelationRuleDefinitionThenItem {
    FieldTypeID: number;
    FieldTypeName: string;
    ValueOptions: SelectItem[] = [];
    Value: string;
}

export class ResponsibilityTypeRelationRuleFormData {
    FieldTypes: ResponsibilityTypeRelationRuleFormDataFieldType[] = [];
    IntersectTypes: SelectItem[] = [];
}
export class ResponsibilityTypeRelationRuleFormDataFieldType {
    value: string;
    label: string;
    type: string;
    isLookup: boolean;
    values: SelectItem[] = [];
}