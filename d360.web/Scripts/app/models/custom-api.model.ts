export class ApiService {
    ID: number;
    UriPrefix: string;
    Name: string;
    Description: string;
}

export class ApiEndpoint {
    ID: number;
    ServiceID: number;
    UriPrefix: string;
    Name: string;
    Description: string;
}

export class ApiVersion {
    ID: number;
    EndpointID: number;
    UriPrefix: string;
    MajorVersion: number;
    MinorVersion: number;
}

export class ApiField {
    Name: string;
    Type: string;
    AllowSelect: boolean;
    AllowSort: boolean;
    AllowFilter: boolean;
    JsonFieldNameOverride: string;
    XmlFieldNameOverride: string;
    EntityID: number;
    FieldTypeID: number;
}

export class ApiUri {
    UriType: string;
    Format: string;
    EntityID: number;
}