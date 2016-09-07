export class SearchResult {
    Desc: string;
    Name: string;
    DisplayName: string;
    Url: string;
    Type: string;       
}

export class SearchFullResult {
    Description: string;
    Group: string;
    ID: number;
    Name: string;
    NormalizedScore: number;
    Score: number;
    Type: string;
    Url: string;

    
}

export class SearchCategories {
    Name: string;
    DisplayName: string;
    ResultCount: number;
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