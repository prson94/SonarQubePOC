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

export class CountObject {
    Count: number;
    IconBackColor: string;
    IconForeColor: string;
    IconText: string;
    Type: string;
    TypeID: number;
    TypeName: string;
}