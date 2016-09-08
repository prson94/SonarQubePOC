export class Resource {    
    DateLastLoggedIn: string;
    Email: string;
    FirstName: string;
    LastName: string;
    Status: string;
    IsAdministrator: boolean;
    ID: number;

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
    ResponsibilityID: number;
    ObjectType: string;
    ObjectID: number;
    ObjectName: string;
    ObjectTypeID: number;
    ObjectTypeName: string;
    ObjectUrl: string;
    ResponsibleObjectType: string;
    ResponsibleObjectID: string;
    FormGroup: boolean;
    Role: string;
    CurrentScore: number;
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
