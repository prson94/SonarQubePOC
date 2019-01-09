import { FieldType } from "./fields.model";

export class MetricAssetViewModel {
    Uid: string;
    ParentUid: string;
    AssetTypeUid: string;
    IsGroup: boolean;
    Name: string;
    Description: string;
    EffectiveDate: string | Date;
    Weight: number;
    ConditionAndOr: string;

    Conditions: MetricAssetVersionConditionViewModel[] = [];
}

export class MetricAssetVersionConditionViewModel {
    FieldTypeID: number;
    Operator: string;
    Values: any;//[] = [];
    
    // Transitive values used for UI logic only.
    FieldType: MetricFieldTypeViewModel;
    FieldTypeName: string;
    ValuesText: string;
    OperatorText: string;
    IsEditMode: boolean;
}

export class MetricFieldTypeViewModel {
    ID: number;
    Name: string;
    Type: string;
    Disabled: boolean = false;
    Values: MetricFieldTypeValueViewModel[] = [];
}
export class MetricFieldTypeValueViewModel {
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
    Map: Map;
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

export class Map {
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

