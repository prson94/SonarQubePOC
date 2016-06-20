export interface IGovernanceService {
    getGovernanceItems(): Promise<GovernanceItem[]>;
}

export class GovernanceItem {
    ID: number;
    Name: string;
}