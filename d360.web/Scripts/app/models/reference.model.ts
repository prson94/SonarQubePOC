export class ReferenceItemType{
    ID: number;
    Name: string;
    DisplayFormat: string;
    Description: string;
    CreatedOn: Date;
    CreatedBy: string;
    UpdatedOn: Date;
    UpdatedBy: string;
}

export class ReferenceItem {
    ID: number;
    ReferenceItemTypeID: number;
    DisplayValue: string;
    CreatedOn: Date;
    CreatedBy: string;
    UpdatedOn: Date;
    UpdatedBy: string;
}