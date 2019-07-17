import { SelectItem } from 'primeng/components/common/api';
import { Observable } from 'rxjs';

export interface IResponsibilityTypeService {
    getResponsibilityTypes(): Observable<ResponsibilityType[]>;
    getResponsibilityType(id: number): Observable<ResponsibilityType>;
    putResponsibilityType(responsibilityType: ResponsibilityType): Observable<any>;
    postResponsibilityType(responsibilityType: ResponsibilityType): Observable<any>;
    deleteResponsibilityType(id: number): Observable<any>;
    getResponsibilityTypeBreakdown(): Observable<ResponsibilityTypeCount[]>;
    getResourceResponsibilityByType(responsibilityTypeId: number): Observable<ResourceResponsibilityTypeCount[]>;
    getResponsibilityTypesByObject(type: string, id: number): Observable<any>;
}

export enum Permission {
    ReadAsset = 1,
    ModifyAsset = 2,
    DeleteAsset = 4,

    ReadAttributes = 8,
    ModifyAttributes = 16,
    DeleteAttributes = 32,

    ReadResponsibilities = 64,
    ModifyResponsibilities = 128,
    DeleteResponsibilities = 256,

    ReadRelationships = 512,
    ModifyRelationships = 1024,
    DeleteRelationships = 2048
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
    ResponsibilityTypeName: string;
    AssetTypeName: string;
    AssetTypeID: number;
    ObjectType: string;
    ObjectID: number;
    PermissionsBitMask: number;
    Permissions: ResponsibilityTypeRelationPermission[] = [];
}

export class ResponsibilityTypeRelation_FormData {
    AllocationOptions: ResponsibilityTypeRelationAllocationOption[] = [];
    PermissionOptions: ResponsibilityTypeRelationPermission[] = [];
}

export class ResponsibilityTypeRelationAllocationOption {
    IsUsed: boolean;
    ID: number;
    Path: string;
}

export class ResponsibilityTypeRelationPermission {
    Value: number;
    ID: string;
    Category: string;
    Name: string;
    Description: number;
    Selected: boolean;

    static hasPermission(permissions: ResponsibilityTypeRelationPermission[], p: Permission): boolean {

        let index = permissions.findIndex(i => i.Value == p);

        if (index >= 0 && index < permissions.length) return true;

        return false;
    }
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
    LastRunOn: string;
}

export class ResponsibilityTypeRelationRule {
    ID: number;
    ResponsibilityTypeID: number;
    Name: string;
    Object: string;
    ObjectID: number;
    ObjectString: string;
    StructuredDefinition: ResponsibilityTypeRelationRuleDefinition;
    Context: string;
    IsVisible: boolean;
    ApplyToType: boolean;
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
    IsBool: boolean = false;
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