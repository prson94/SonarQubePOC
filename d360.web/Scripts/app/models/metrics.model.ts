import { FieldType } from "./fields.model";
import { State, AssetTypeClass } from "./asset.model";
import { OperatorModel, Operator } from "./operator.model";
import { FieldCondition } from "./field-condition-grid.models";

export class MetricAssetViewModel {
    Uid: string;
    ParentUid: string;
    AllocationUid: string;
    AssetTypeUid: string;
    IsGroup: boolean;
    Name: string;
    Definition: MetricAssetDefinitionViewModel;
    Description: string;
    EffectiveDate: Date;
    Weight: number;
    AdjustedWeight: number;
    Threshold: number;
    MatchConditionsOnly: boolean = false;
    ConditionGroups: MetricAssetVersionConditionViewModel[] = [];
    VersionCount: number;
    HasResults: boolean;

    // Only used in UI.
    HasThreshold: boolean = false;
}

export class MetricAssetHistoryViewModel {
    Uid: string;
    Version: number;
    Name: string;
    Description: string;
    EffectiveDate: Date;
    EffectiveEndDate: Date;
    Weight: number;
    MatchConditionsOnly: boolean = false;
    ConditionGroups: MetricAssetVersionConditionViewModel[] = [];
    HasResults: boolean;
    Definition: MetricAssetDefinitionViewModel;
}
type MatchTypeString = 'All' | 'Any';
export class MetricAssetVersionConditionViewModel {
    Uid: string;
    Position: number;
    Threshold: number;
    Weight: number;
    MatchType: MatchTypeString;

    ConditionItems: MetricAssetVersionConditionItemViewModel[] = [];

    //used for the fieldconditiongrids
    conditionItemFields: FieldCondition[] = [];
    DisplayOrder: number;
    DisplayThreshold: number;
    DisplayWeight: number;

}

export class MetricAssetVersionConditionItemViewModel {
    Uid: string;
    ConditionType: MetricConditionType;
    ConditionFieldTypeName: string;
    ConditionIntersectTypeUid: string;
    Operator: Operator;
    Values: string[] = [];

    // Transitive values used for UI logic only.
    FieldType: MetricFieldTypeViewModel;
    FieldTypeName: string;
    ValuesText: string;
    OperatorText: string;
    IsEditMode: boolean; 
    operatorOptions: any[];
    lookupOptions: any[];
    SingleValue: any; //For non-list fields
}

export class MetricAssetDefinitionViewModel {
    DataQuality: MetricAssetDefinitionDataQualityViewModel;
    Governance: MetricAssetDefinitionGovernanceViewModel;
}
export class MetricAssetDefinitionDataQualityViewModel {
    ResultOperation: MetricRuleResultOperation;
    ResultPathUid: string;
    FilterMatchType: MetricMatchType;
    Filters: MetricAssetDefinitionDataQualityFilterViewModel[] = [];
}
export class MetricAssetDefinitionDataQualityFilterViewModel {
    AssetTypeUid: string;
    FieldTypeName: string;
    Operator: Operator;
    Values: string[];
}
export class MetricAssetDefinitionGovernanceViewModel {
    Check: MetricGovernanceCheckType;

    Field: MetricAssetDefinitionGovernanceFieldViewModel;
    Predicate: MetricAssetDefinitionGovernancePredicateViewModel;
    Relation: MetricAssetDefinitionGovernanceRelationViewModel;
    Owner: MetricAssetDefinitionGovernanceOwnerViewModel;
    External: MetricAssetDefinitionGovernanceExternalViewModel;
}
export class MetricAssetDefinitionGovernanceExternalViewModel {
    UpdateFrequency: MetricUpdateFrequency;
    Instructions: string;
}
export class MetricAssetDefinitionGovernanceFieldViewModel {
    FieldTypeName: string;
    Operator: Operator;
    Values: string[];
}
export class MetricAssetDefinitionGovernancePredicateViewModel {
    PredicateUid: string;
    Operator: Operator;
}
export class MetricAssetDefinitionGovernanceRelationViewModel {
    IntersectTypeUid: string;
    Operator: Operator;
    Values: string[];
}
export class MetricAssetDefinitionGovernanceOwnerViewModel {
    ResponsibilityTypeUid: string;
    Operator: Operator;
}

export class MetricFieldTypeViewModel {
    AssetTypeUid: string;
    AssetTypeName: string;
    ID: number;
    ApiName: string;
    Name: string;
    Type: string;
    Disabled = false;
    Values: MetricAssetVersionConditionItemFieldValueViewModel[] = [];
}

export class MetricAssetVersionConditionItemFieldValueViewModel {
    Value: string;
    Text: string;
}

export class MetricPathOptionSegmentViewModel {
    AssetTypeUid: string;
    Name: string;
    Path: string;
}

export class MetricPathOptionViewModel {
    Uid: string;
    State: State;
    Path: string;
    Segments: MetricPathOptionSegmentViewModel[];

    label: string;
    value: string;
}

export class Group {
    ID: number;
    ParentID: number;
    Name: string;
    Description: string;
    Weight: number;
    EffectiveStartDate: string;
    EffectiveEndDate: string;
    SourceID: string;

    Children: Group[] = [];
}

export class GroupForm {
    Group: Group = new Group();
    Children: Group[] = [];
}

export class MapForm {
    Map: MetricMap;
    Items: Item[] = [];
    ObjectTypes: any[] = [];
    Conditions: Condition[] = [];

    AssetTypes: any[] = [];
}

export class ConditionForm {
    Condition: Condition;
    Fields: FieldType[] = [];
}


export class Item {
    ID: number;
    Name: string;
    Description: string;
    EffectiveStartDate: string;
    EffectiveEndDate: string;
    SourceID: string;
}

export class MetricMap {
    ID: number;
    GroupID: number;
    ItemID: number;
    Object: string;
    ObjectID: number;
    Weight: number;
    EffectiveStartDate: string | Date;
    EffectiveEndDate: string | Date;

    itemName: string;
    objectName: string;

    EffectiveDate: string | Date;
    AssetTypeID: number;
}

export class Condition {
    MapID: number;
    FieldTypeID: number;
    AndOr: string;
    Operator: OperatorModel;
    Value: string;

    fieldName: string;
    operatorName: string;
    andOrName: string;
}


export class ScoreTypeAllocation {
    uid: string;
    assetClassName: AssetTypeClass;
    assetTypeUid: string;
    assetTypePath: string;
    scoreType: ScoreType;
    state: State;
    hasMeasure: boolean;
    hasDisabledMeasure: boolean;
    hasField: boolean;
    isExternallyCalculated: boolean;
    lowerThreshold: number;
    upperThreshold: number;

    icon: string; // Loaded via admin page, not from API.
}

export class ScoreTypeAllocationFormatted {
    uid: string;
    assetClassName: string;
    assetTypeUid: string;
    assetTypePath: string;
    scoreType: string;
    state: State;
    hasMeasure: boolean;
    hasDisabledMeasure: boolean;
    hasField: boolean;
    isExternallyCalculated: string;
    lowerThreshold: number;
    upperThreshold: number;
    formattedThreshold: number;
}

export enum ScoreType {
    Governance = 1,
    DataQuality = 2,
    Perceptional = 3
}

export const ScoreTypeInfo = new Map<string, string>([
    ["Governance", "Governance Score"],
    ["DataQuality", "Data Quality Score"],
    ["Perceptional", "Perception Score"]
]);

export enum MetricUpdateFrequency {
    None = 0,
    Hourly = 1,
    Daily = 2,
    Weekly = 3,
    Monthly = 4,
    Quarterly = 5,
    Annually = 6
}

export enum MetricRuleResultOperation {
    Average = 1,
    Minimum = 2,
    Maximum = 3
}

export enum MetricMatchType {
    Any = 1,
    All = 2
}

export enum MetricGovernanceCheckType {
    External = 0,
    Field = 1,
    Owner = 2,
    Predicate = 3,
    Relation = 4
}

export enum MetricConditionType {
    NotApplicable = 0,
    And = 1,
    Or = 2
}