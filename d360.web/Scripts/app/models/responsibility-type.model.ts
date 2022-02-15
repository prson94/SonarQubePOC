import { SelectItem } from 'primeng/api';
import { Observable } from 'rxjs';
import { AssetTypeClassApiModel } from "./asset.model";

export interface IResponsibilityTypeService {
    getResponsibilityTypes(): Observable<ResponsibilityType[]>;
    getResponsibilityType(id: number): Observable<ResponsibilityType>;
    putResponsibilityType(responsibilityType: ResponsibilityType): Observable<any>;
    postResponsibilityType(responsibilityType: ResponsibilityType): Observable<any>;
    deleteResponsibilityType(uid: string, cascade?: boolean): Observable<any>;
    getResponsibilityTypeBreakdown(): Observable<ResponsibilityTypeCount[]>;
    getResourceResponsibilityByType(responsibilityTypeUid: string): Observable<ResourceResponsibilityTypeCount[]>;
    getResponsibilityTypesByObject(type: string, id: number): Observable<any>;
}

export enum Permission {
    ReadAsset = 1,
    AddAsset = 2,
    DeleteAsset = 4,
    EditAsset = 8,

    ReadResponsibilities = 32,
    AddResponsibilities = 64,
    DeleteResponsibilities = 128,
    EditResponsibilities = 256,

    ReadRelationships = 1024,
    AddRelationships = 2048,
    DeleteRelationships = 4096,
    EditRelationships = 8192,
}

export class ResponsibilityTypeRelationPermission {
    Value: number;
    ID: string;
    Category: string;
    Name: string;
    Description: number;
    Selected: boolean;

    static hasPermission(permissions: ResponsibilityTypeRelationPermission[], p: Permission): boolean {

        let index = permissions.findIndex((i) => i.Value === p);

        if (index >= 0 && index < permissions.length) {
            return true;
        }

        return false;
    }
}

export class ResponsibilityType {
    ID: number;
    Name: string;
    Description: string;
    UpdatedOn: string;
    UpdatedBy: number;
    ResponsibilityTypeRelations: ResponsibilityTypeRelation[] = [];
    AllocationsList: SelectItem[] = [];
    uid: string;
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

export class ResponsibilityTypeAllocation {
    ResponsibilityTypeUid: string;
    ResponsibilityTypeName: string;
    AssetTypeUid: string;
    AssetTypeName: string;
    AssetTypePath: string;
    AssetClass: AssetTypeClassApiModel;
    PermissionsMask: number;
    Permissions: ResponsibilityTypeRelationPermission[];
}

export class ResponsibilityTypeAllocationPost {
    AssetTypeUid: string;
    Permissions: number[];
}

export class ResponsibilityTypeRelation_FormData {
    AllocationOptions: ResponsibilityTypeRelationAllocationOption[] = [];
    PermissionOptions: ResponsibilityTypeRelationPermission[] = [];
}

export class ResponsibilityTypeRelationAllocationOption {
    IsUsed: boolean;
    ID: number;
    Uid: string;
    Path: string;
}

export class ResponsibilityTypeCount {
    Count: number;
    ResponsibilityType: string;
    ResponsibilityTypeID: number;
    ResponsibilityTypeUID: number;
}

export class ResourceResponsibilityTypeCount {
    FirstName: string;
    LastName: string;
    OwnedItemCount: number;
    ResourceID: number;
    ResponsibilityType: string;
    ResponsibilityTypeID: number;
    ResponsibilityTypeUID: string;
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
    AssetTypeUid: string;
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
    IntersectTypeValueOptions: (SelectItem & { assetUid: string })[] = [];
    IsLookup: boolean = false;
    IsBool: boolean = false;
    IsloadValuesForIntersectType: boolean = false;
}

export class ResponsibilityTypeRelationRuleDefinitionWhenTestRow {
    Name: string;
}

export class ResponsibilityTypeRelationRuleDefinitionThenTestRow {
    Name: string;
}

export class ResponsibilityTypeRelationRuleDefinitionThen {
    Object: string;
    ObjectID: number;
    Conditions: ResponsibilityTypeRelationRuleDefinitionThenItem[] = [];
    MatchType: string = "and";
}

export class ResponsibilityTypeRelationRuleDefinitionThenItem {
    FieldTypeID: number;
    FieldTypeName: string;
    ValueOptions: SelectItem[] = [];
    Value: string;
}

export class ResponsibilityTypeRelationRuleFormData {
    FieldTypes: ResponsibilityTypeRelationRuleFormDataFieldType[] = [];
    IntersectTypes: (SelectItem & { uid: string })[] = [];
}
export class ResponsibilityTypeRelationRuleFormDataFieldType {
    value: number;
    label: string;
    type: string;
    isLookup: boolean;
    values: (SelectItem & { assigneeUid?: string })[] = [];
    assigneeTypeUid?: string;
    fieldTypeName?: string;
}

export class ResponsibilityTypeRelationRuleV2 {
    AssetTypeUid: string;
    Definition: ResponsibilityTypeRelationRuleDefinitionV2
}

export class ResponsibilityTypeRelationRuleDefinitionV2 {
    When: RuleWhenV2[];
    Then: RuleThenWrapperV2[];
}

export interface RuleWhenV2 {
    Field?: RuleFieldConditionV2;
    Relation?: RuleRelationConditionV2;
}

export interface RuleFieldConditionV2 {
    ApiName: string;
    Value: string;
}

export interface RuleRelationConditionV2 {
    IntersectTypeUid: string | undefined;
    AssetUid: string | undefined;
}

export type ResponsibilityRuleTestResponseModel = {
    pageNum?: number;
    pageSize?: number;
    items: {
        uid: string;
        path: string;
    }[];
}

export interface RuleThenWrapperV2 {
    AssigneeTypeUid?: string;
    MatchType?: 'and' | 'or';
    Conditions: RuleThenV2[];
}

export interface RuleThenV2 {
    Field?: RuleFieldConditionV2;
    Assignee?: RuleAsigneeConditionV2;
}

export interface RuleAsigneeConditionV2 {
    Uid?: string;
}