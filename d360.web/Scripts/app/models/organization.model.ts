export class OrganizationType {
    ID: number;
    Name: string;
    Description: string;
    AssetTypeID: number;
    OrganizationCount: string;
}

export class Organization {
    Name: string;
    ID: number;
    Accepted: boolean;
    AcceptedBy: number;
    DateAccepted: string;
    AdministratorEmail: string;

    //part of view model
    AcceptedByName: string;
}

export enum ContractType {
    TermsOfUse = 1
}

export class Contract {
    //part of base
    ID: number;
    OrganizationID: number;
    Body: string;
    ContractType: ContractType;
    Title: string;

    //part of view model
    ContractTypeName: string;
    ContractTypeDescription: string;
    OrganizationName: string;
}

export class OrganizationDomain {
    ID: number;
    OrganizationID: number;
    Domain: string;
}

export class OrganizationInvitation {
    ID: number;
    OrganizationID: number;
    Email: string;

    //part of view model
    OrganizationName: string;
}

export class OrganizationResource {
    OrganizationID: number;
    ResourceID: number;
    Accepted: boolean;
    DateAccepted: Date;

    //part of view model
    OrganizationName: string;
    FirstName: string;
    LastName: string;
    Email: string;
    Status: string;
    DateLastLoggedIn: Date;
}