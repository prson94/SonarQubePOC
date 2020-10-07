export class HelpResource {
    Name: string;
    Description: string;
    Url: string;
    Type: number;
    ID: number;
}

export class Resource {    
    DateLastLoggedIn: string;
    Email: string;
    FirstName: string;
    LastName: string;
    Status: string;
    IsAdministrator: boolean;
    ID: number;
    Uid: string;

    public FullName() : string {
        return `${this.FirstName} ${this.LastName}`;
    }
}

export class ResourceAPICredentials {
    PublicKey: string;
    PrivateKey: string;
    Token: string;
}

export class CountObject {
    Count: number;
    IconBackColor: string;
    IconForeColor: string;
    IconText: string;
    Type: string;
    TypeID: number;
    TypeName: string;
}

export class ResponsibilityDetailForResource {
    ResponsibilityTypeID: number;
    SecurityAsset: string;
    SecurityAssetID: number;
    SecurityAssetName: string;
    Type: string;
    ID: number;
    TypeName: string;
    Object: string;
    ObjectName: string;
    ObjectID: number;
    ResponsibilityTypeName: string;
    Via: string;
}

export class FollowingDetailForResource {
    ID: number;
    CurrentScore: number;
    Name: string;
    ObjectID: number;
    ObjectType: string;
    OpenEventCount: number;
    Url: string;
}

export class MulitSelectResourceData {
    results: any;
    total: number;
}