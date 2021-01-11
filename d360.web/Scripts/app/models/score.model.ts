import { ScoreType } from "./metrics.model";

export class PointBreakdown {
    Uid: string;
    ParentUid: string;
    IsGroup: boolean;
    Name: string;
    Description: string;
    Weight: number;
    AdjustedWeight: number;
    AdjustedMaxWeight: number;
    Value: boolean;
    DecimalValue: number;
    EffectiveDate: string;
    EndDate: string;
    ScoreType: ScoreType;
    Conditions: PointBreakdownCondition[];
    Measures: PointBreakdown[];


    //ui data
    _finalScore: number = 0;
    _isSelected: boolean = false;
    _badgeStyle: string = 'default';
    _isCollapsed: boolean = false;
    _adjustedGroupWeight: number = 0;
    _adjustedWeight: number = 0;
    _adjustedMaxWeight: number = 0;
    _measureSumWeight: number = 0;
}

export class PointBreakdownCondition {
    FieldName: string;
    Operator: string;
    Value: string;
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