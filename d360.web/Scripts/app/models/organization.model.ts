import { State } from "./asset.model";

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
    ID: number;
    OrganizationID: number;
    Body: string;
    ContractType: ContractType;
    Title: string;
    UpdatedOn: string;
    UpdatedBy: number;
    CreatedOn: string;
    CreatedBy: number;
    State: State = State.Active;
    PublishedOn: string;
}

export class ContractAcceptance {
    ID: number;
    ResourceID: number;
    Accepted: boolean;
    AcceptedOn: string;
    ContractID: number;
    OrganizationID: number;
}

export class ContractAcceptanceDetail {
    ID: number;
    ResourceID: number;
    Accepted: boolean;
    AcceptedOn: string;
    ContractID: number;
    OrganizationID: number;
    ResourceName: string;
    ContractName: string;
}


export class ContractDetail {
    ID: number;
    Title: string;
    Body: string;
    OrganizationID: number;
    ContractType: ContractType;
    OrganizationName: string;
    ContractTypeName: string;
    ContractTypeDescription: string;
    PublishedOn: string;
    UpdatedOn: string;
    UpdatedBy: number;
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