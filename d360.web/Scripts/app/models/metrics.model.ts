export class Group {
    ID: number;
    ParentID: number;
    Name: string;
    Description: string;
    Weight: number;
    EffectiveStartDate: string;
    EffectiveEndDate: string;
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
}


