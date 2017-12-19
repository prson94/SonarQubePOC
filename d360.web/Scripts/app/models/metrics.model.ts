export class Group {
    ID: number;
    ParentID: number;
    Name: string;
    Description: string;
    Weight: number;
    EffectiveStartDate: string;
    EffectiveEndDate: string;
}

export class GroupForm {
    Group: Group = new Group();
    Children: Group[] = [];
}

export class Item {
    ID: number;
    Name: string;
    Description: string;
    EffectiveStartDate: string;
    EffectiveEndDate: string;
}

export class Map {
    ID: number;
    GroupID: number;
    ItemID: number;
    Object: string;
    ObjectID: number;
    Weight: number;
    EffectiveStartDate: string;
    EffectiveEndDate: string;

    itemName: string;
    objectName: string;
}




