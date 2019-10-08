

export enum State {
    Unknown = -1,
    PendingAdd = 0,
    Active = 1,
    PendingDelete = 2,
    Deleted = 3,
    InActive = 4
}

export class AssetEditorModel {
    Uid: string;
    ParentUid: string;
    Fields: any;
}

export class AssetDetail {
    AssetTypeID: number;
    AssetTypeName: string;
    CreatedOn: Date;
    DisplayValue: string;
    ID: number;
    Object: string;
    ObjectID: number;
    State: number;
    Type: string;
    TypeID: number;
    UpdatedOn: Date;
}

export class AssetTypeMetricModel {
    Uid: string;
    Name: string;
    Class: string
}

export class AssetTypeEditorModel {
    AssetType: AssetType;
    ParentUid: string;
    Predicates: any[];
    Tokens: any[];
    Parents: any[];
}

export enum AssetTypeClass {
    BusinessAsset = 1,
    Model = 2,
    Fusion = 3,
    FusionAttribute = 4,
    FusionQuery = 4,
    AttributeGroup = 5,
    Policy = 6,
    Rule = 7,
    TechnicalAsset = 8,
    Reference = 9,
    Organization = 10,
    ReferenceItemType = 14
}

export class AssetType {
    Uid: string;
    Name: string;
    Class: AssetTypeClass;
    Description: string;
    AutoDisplayDescription: boolean;
    DisplayFormat: string;
    ParentUid: string;
    Notes: string;
    UseAsTransformation: boolean;
    CanOwnFusion: boolean;
    IconStyle: IconStyle = new IconStyle();
    Hierarchy: Hierarchy = new Hierarchy();

}

export class IconStyle {
    ForeColor: string;
    BackColor: string;
}

export class Hierarchy {
    MaximumDepth: number;
    PredicateUid: string;
}

