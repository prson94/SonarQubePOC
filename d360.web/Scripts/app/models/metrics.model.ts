import { FieldType } from "./fields.model";

export class Group {
    ID: number;
    ParentID: number;
    Name: string;
    Description: string;
    Weight: number;
    EffectiveStartDate: string;
    EffectiveEndDate: string;
    SourceID: string;
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

