import { ScoreType } from "./metrics.model";

export class PointBreakdown {
    Uid: string;
    ParentUid: string;
    Level: number;
    IsGroup: boolean;
    Name: string;
    Description: string;
    Weight: number;
    Value: boolean;
    EffectiveDate: string;
    ScoreType: ScoreType;
}

export class ScorePoint {
    Score: number;
    Date: string;
    ScoreType: ScoreType;
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