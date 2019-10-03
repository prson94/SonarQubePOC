

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

export class AssetTypeOld {
    Uid: string;
    ID: number;
    Name: string;
    Description: string;
    Class: AssetTypeClass;
    DisplayFormat: string;
    //    State: State;
    Hierarchical: boolean;
    HierarchyMaximumDepth: number;
    UseAsTransformation: boolean;
    Object: string;
    ObjectID: number;
    CreatedBy: number;
    CreatedOn: string;
    UpdatedBy: number;
    UpdatedOn: string;
}

export class AssetTypeEditorModel {
    SelectedPredicateUid: string;
    ParentUid: string;
    Predicates: any[];
    Tokens: any[];
    AssetType: AssetType;
    Parents: any[];

    // TODO: Extra fields to remove to appropriate classes when fully converted over to Asset.
    CanOwnFusion: boolean;              //ArtifactType
    ShowNameInTree: boolean;            //AttributeType
    TypeClassID: number;                //AttributeType.AttributeTypeCategoryID, TaxonomyType.TaxonomyTypeClassID
    Assignable: boolean;                //FusionAttributeType
    ScanEnabled: boolean;               //FusionAttributeType
    Query: string;                      //FusionQueryAttributeType
    TopLevelTypeID: number;             //FusionTypeID,etc.
}

export enum AssetTypeClass {
    Business = 1,
    Model = 2,
    Fusion = 3,
    FusionAttribute = 4,
    FusionQuery = 4,
    AttributeGroup = 5,
    Policy = 6,
    Rule = 7,
    Technical = 8,
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
    IconStyle: IconStyle;
    Hierarchy: Hierarchy;

}

export class IconStyle {
    ForeColor: string;
    BackColor: string;
}

export class Hierarchy {
    MaximumDepth: number;
    PredicateUid: string;
}

