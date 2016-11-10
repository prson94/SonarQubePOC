export class PolicyType {
    Name: string;
    Description: string;   
    ID: number;
    PolicyTypeClass: string;
    PolicyTypeClassID: number;
}

export class Policy {
    ID: number;
    ParentID: number;
    Name: string;
    Description: string;
}