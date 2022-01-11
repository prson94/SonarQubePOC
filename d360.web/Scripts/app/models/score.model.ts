import { AssetTypeClass } from "./asset.model";
import { ScoreType } from "./metrics.model";
import { Operator } from "./operator.model";

//#region Evidence Models

export class DataQualityScoreItemEvidenceItemRollupPathModel {
    Uid: string;
    AssetPath: string;
    AssetTypePath: string;
    Predicate: string;
    Position: number;
}

export class DataQualityEvidenceItemModel {
    RollupPath: DataQualityScoreItemEvidenceItemRollupPathModel[];
    ResultUid: string;
    OwningAssetUid: string;
    OwningAssetPath: string;
    OwningAssetTypePath: string;
    OwningAssetDisplayPath: string;
    EvaluatedAssetUid: string;
    EvaluatedAssetPath: string;
    EvaluatedAssetTypePath: string;
    EvaluatedAssetDisplayPath: string;
    EvaluatedAssetClass: AssetTypeClass;
    EffectiveDate: Date;
    RunDate: Date;
    TotalCount: number;
    PassCount: number;
    FailCount: number;
    PassFraction: number;    
}

export class DataQualityEvidenceModel {
    pageSize: number;
    pageNum: number;
    total: number;
    items: DataQualityEvidenceItemModel[]
}

//#endregion Evidence Models

export class PointBreakdown {
    Uid: string;
    ParentUid: string;
    ScoreItemUid: string;
    IsGroup: boolean;
    Name: string;
    Description: string;
    Threshold: number;
    Weight: number;
    AdjustedWeight: number;
    AdjustedMaxWeight: number;
    DisplayWeight: number;
    DisplayMaxWeight: number;
    Value: boolean;
    DecimalValue: number;
    EffectiveDate: string;
    EndDate: string;
    ScoreType: ScoreType;
    MatchConditionsOnly;
    Conditions: PointBreakDownConditionItem[];
    Measures: PointBreakdown[];
    ConditionUid: string;
    OtherConditions: string[];

    //ui data
    _finalScore: number = 0;
    _isSelected: boolean = false;
    _badgeStyle: string = 'default';
    _isCollapsed: boolean = false;
    _groupDisplayMaxWeight: number = 0;
    _groupDisplayWeight: number = 0;
    _rawWeightSum: number = 0;
}

export class PointBreakDownConditionItem {
    Uid: string;
    Weight: number;
    MatchType: number;
    Position: number;
    ConditionItems: PointBreakdownCondition[]; 
}

export class PointBreakdownCondition {
    FieldName: string;
    Operator: Operator;
    Value: string;

    //display only
    _formattedValue: string;

}

export class ScorePoint {
    Score: number;
    EndDate: string;
    EffectiveDate: string;
    ScoreType: ScoreType;
    ScoreProgression: number;
}

export class AverageScore {
    AverageScore: number;
    Object: string;
    ObjectID: number;
    ObjectName: string;
    ObjectScore: string;
    ObjectType: string;
    ObjectTypeID: number;
    ObjectTypeName: string;
}