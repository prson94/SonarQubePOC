export class AttributeType {
    ID: number;
    Name: string;
    ParentID: number;
    ShowNameInTree: boolean;
    AttributeTypeCategoryID: number;
    TextFormatString: string;
}

export class AttributeTypeAllocation {
    AllowMultipleEntries: boolean;
    AttributeTypeID: number;
    ObjectID: number;
    ObjectName: string;
    ObjectType: string;
    Required: boolean;
}