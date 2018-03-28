
export class FusionAttributePagedResults {
    total: number;
    results: any[];
}

export class FusionAttributeFieldValue {
    Name: string;
    Value: string;
}

export class FusionAttributeValueDetails {
    Name: string;
    TextPath: string;
    Fields: FusionAttributeFieldValue[];
    FusionID: number;
    FusionAttributeTypeID: number;
    AssetID: number;
}

export class FusionAttributeFilter {
    dataField: string;
    value: string;
    condition: string = 'CONTAINS';
    columnType: string;
}