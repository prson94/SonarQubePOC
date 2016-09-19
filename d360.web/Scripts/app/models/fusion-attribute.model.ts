
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
}