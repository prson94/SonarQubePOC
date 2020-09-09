import { FieldType } from "./fields.model";
import { State, AssetTypeClass } from "./asset.model";

export class MetricAssetViewModel {
    Uid: string;
    ParentUid: string;
    AllocationUid: string;
    AssetTypeUid: string;
    IsGroup: boolean;
    Name: string;
    Description: string;
    EffectiveDate: Date;
    Weight: number;
    Threshold: number;
    UpdateFrequency: MetricUpdateFrequency;
    MatchConditionsOnly: boolean;
    ConditionGroups: MetricAssetVersionConditionViewModel[] = [];
    VersionCount: number;
    HasResults: boolean;
}

export class MetricAssetHistoryViewModel {
    Uid: string;
    Version: number;
    Name: string;
    Description: string;
    EffectiveDate: Date;
    EffectiveEndDate: Date;
    Weight: number;
    ConditionGroups: MetricAssetVersionConditionViewModel[] = [];
    HasResults: boolean;
}

export class MetricAssetVersionConditionViewModel {
    Uid: string;
    Position: number;
    Threshold: number;
    Weight: number;
    MatchType: MetricMatchType;

    ConditionItems: MetricAssetVersionConditionItemViewModel[] = [];
}

export class MetricAssetVersionConditionItemViewModel {
    Uid: string;
    ConditionType: MetricConditionType;
    ConditionFieldTypeName: string;
    ConditionIntersectTypeUid: string;
    Operator: string;
    Values: MetricAssetVersionConditionItemValueViewModel[] = [];

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

export class MetricFieldTypeViewModel {
    ApiName: string;
    Name: string;
    Type: string;
    Disabled = false;
    Values: MetricAssetVersionConditionItemFieldValueViewModel[] = [];
}
export class MetricAssetVersionConditionItemValueViewModel {
    Value: string; 
}

export class MetricAssetVersionConditionItemFieldValueViewModel {
    Value: number;
    Text: string;
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
    Operator: string;
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

export enum MetricMatchType {
    Any = 1,
    All = 2
}

export enum MetricConditionType {
    And = 1,
    Or = 2
}