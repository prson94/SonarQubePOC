export class SearchResult {
    Desc: string;
    Name: string;
    DisplayName: string;
    Url: string;
    Type: string; 
    ID: string;
    Icon: string;
}

export class SearchFullResult {
    Description: string;
    Group: string;
    ID: string;
    Name: string;
    NormalizedScore: number;
    Score: number;
    Type: string;
    Url: string;
    Icon: string;
    Uid: string;
    
}

export class SearchCategories {
    Name: string;
    DisplayName: string;
    ResultCount: number;
    Categories: any[];
}

export class SearchResultInfo {
    ElapsedMS: number;
    Matches: number;
    Results: SearchFullResult[];
}

export class SearchResultsObject {
    Categories: SearchCategories[];
    Result: SearchResultInfo;
}

export class AdvancedSearchFilter {
    constructor(field?: string, value?:string) {
        this.field = field;
        this.value = value;
    }

    field: string;
    value: string;
    exact: boolean = false;
    connector: string = 'and';
}