export class ReferenceItemType{
    ID: number;
    Name: string;
    DisplayFormat: string;
    Description: string;
    SourceNotes: string;
    CreatedOn: Date;
    CreatedBy: number;
    UpdatedOn: Date;
    UpdatedBy: number;
    AssetTypeID: number;
}

export class ReferenceItem {
    ID: number;
    AssetTypeID: number;
    ReferenceItemTypeID: number;
    DisplayValue: string;
    CreatedOn: Date;
    CreatedBy: number;
    UpdatedOn: Date;
    UpdatedBy: number;
}