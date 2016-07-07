export class FusionType {
    ID: number;
    Name: string;
    Description: string;
    UpdatedOn: string;
    UpdatedBy: number;
}

export class FusionAttributeType {
    ID: number;
    ParentID: number;
    FusionTypeID: number;
    Assignable: boolean;
    Name: string;
    Path: string;
    TextPath: string;
    UpdatedOn: string;
    UpdatedBy: number;
}

export class FusionConfiguration {
    ID: number;
    Name: string;
    Description: string;
    FusionTypeID: number;
    FusionType: string;
    Enabled: boolean;
}

export class FusionFilter {
    FusionID: number;
    FusionAttributeTypeID: number;
    Filter: string;
    Name: string; 

}
