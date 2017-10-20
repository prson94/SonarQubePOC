export class MapType {
    ID: number;
    Name: string;
    Description: string;
    CreatedOn: string;
    CreatedBy: number;
    UpdatedOn: string;
    UpdatedBy: number;

    createdByName: string;
    updatedByName: string;

    MapTypeOrders: MapTypeOrder[] = [];
}

export class MapTypeOrder {
    MapTypeID: number;
    IntersectTypeID: number;
    Order: number;
}

export class MapTypeTemplate {
    ID: number;
    MapTypeID: number;
    Name: string;

    Items: MapTypeTemplateItem[] = [];
}

export class MapTypeTemplateItem {
    ID: number;
    MapTypeTemplateID: number;
    IntersectTypeID: number;
    IsRequired: boolean = false;
}