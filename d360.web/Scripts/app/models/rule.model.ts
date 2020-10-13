import { AssetGridObject } from "../components/assets-grid/asset-grid.model";

export class RuleType {
    ID: number;
    Name: string;
    Description: string;
    HasDashboards: boolean;
    AllowAttributes: boolean;
    AutoDisplayDescription: boolean;
    HasCustomExportTemplates: boolean;
    AssetTypeUID: string;
    AssetTypeID: number;
    HasWorkflow: boolean;
    uid: string;

    public static AsGridObject(ruleType: RuleType): AssetGridObject {
        var ago = new AssetGridObject();
        ago.AssetTypeUID = ruleType.AssetTypeUID;
        ago.AutoDisplayDescription = ruleType.AutoDisplayDescription;
        ago.Description = ruleType.Description;
        ago.HasCustomExportTemplates = ruleType.HasCustomExportTemplates;
        ago.ID = ruleType.ID;
        ago.Name = ruleType.Name;
        ago.Object = 'Rule';
        ago.ObjectType = 'RuleType';
        return ago;
    }
}

export class Rule {
    Name: string;
    ID: number;
    Description: string;
    Measurement: string;
    Purpose: string;
    Resolution: string;
    SourceID: number;
}

export class RuleResult {
    ID: number;
    RuleID: number;
    EffectiveDate: Date;
    RowsPassed: number;
    RowsFailed: number;
    PassFraction: number;
    FailFraction: number;
    Passed: boolean;
}

export class RuleDetail {
    Name: string;
    ID: number;
    UID: string;
    AssetID: number;
    Description: string;
    IconBackColor: string;
    IconForeColor: string;
    IconText: string;
    ParentID: number;
    ParentType: string;
    PluralizedName: string;
    TextPath: string;
    TypeName: string;
    Type: string;
    TypeID: number;
    Url: string;
}

export class RuleResultPagedResults {
    pageSize: number;
    pageNum: number;
    total: number;
    items: RuleResultItems[];
}

export class RuleResultItems {
    ResultUid: string;
    OwningAssetUid: string;
    EvaluatedAssetUid: string;
    EvaluatedAssetPath: string;
    EvaluatedAssetTypePath: string;
    EvaluatedAssetDisplayPath: string;
    EvaluatedAssetClass: string;
    EffectiveDate: Date;
    RunDate: Date;
    TotalCount: number;
    PassCount: number;
    FailCount: number;
    PassFraction: number;
    Passed: boolean;
}

export class RuleResultFilter {
    dataField: string;
    value: string;
    condition: string = 'CONTAINS';
}