export interface IDomainService {
    getDomains(): Promise<DomainType[]>;
}

export class DomainType {
    ID: number;
    Name: string;
    Description: string;
}