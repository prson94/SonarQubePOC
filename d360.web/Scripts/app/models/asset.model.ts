export class AssetTypeMetricModel {
    Uid: string;
    Name: string;
    Class: string
}

export class AssetTypeEditorModel {
    IconBackColor: string;
    IconForeColor: string;
    SelectedPredicateID: number;
    ParentID: number;
    Predicates: any[];
    Tokens: any[];
    AssetType: AssetType;
    Parents: any[];

    // TODO: Extra fields to remove to appropriate classes when fully converted over to Asset.
    CanOwnFusion: boolean;              //ArtifactType
    AutoDisplayDescription: boolean;    //ArtifactType
    ShowNameInTree: boolean;            //AttributeType
    TypeClassID: number;                //AttributeType.AttributeTypeCategoryID, TaxonomyType.TaxonomyTypeClassID
    Assignable: boolean;                //FusionAttributeType
    ScanEnabled: boolean;               //FusionAttributeType
    Query: string;                      //FusionQueryAttributeType
    TopLevelTypeID: number;             //FusionTypeID,etc.
    Notes: string;                      //ReferenceItemType
}

export enum AssetTypeClass {
    Glossary = 1,
    Model = 2,
    Fusion = 3,
    FusionAttribute = 4,
    FusionQuery = 4,
    AttributeGroup = 5,
    Policy = 6,
    Rule = 7,
    Map = 8,
    Reference = 9,
    Organization = 10,
    ReferenceItemType = 14
}

export enum State {
    Unknown = -1,
    PendingAdd = 0,
    Active = 1,
    PendingDelete = 2,
    Deleted = 3,
    InActive=4
}

export class AssetType {
    ID: number;
    Name: string;
    Description: string;
    Class: AssetTypeClass;
    DisplayFormat: string;
    State: State;
    Hierarchical: boolean;
    HierarchyMaximumDepth: number;
    Object: string;
    ObjectID: number;
    CreatedBy: number;
    CreatedOn: string;
    UpdatedBy: number;
    UpdatedOn: string;
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