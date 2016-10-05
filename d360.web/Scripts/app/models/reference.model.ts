export class ReferenceItemType{
    ID: number;
    Name: string;
    DisplayFormat: string;
    Description: string;
    CreatedOn: Date;
    CreatedBy: number;
    UpdatedOn: Date;
    UpdatedBy: number;
}

export class ReferenceItem {
    ID: number;
    ReferenceItemTypeID: number;
    DisplayValue: string;
    CreatedOn: Date;
    CreatedBy: number;
    UpdatedOn: Date;
    UpdatedBy: number;
}