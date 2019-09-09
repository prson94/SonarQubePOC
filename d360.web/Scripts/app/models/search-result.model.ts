export class SearchResultTags {
    Uid: string;
    Value: string;
}

export class SearchResult {
    Name: string;
    DisplayName: string;
    Group: string;
    Type: string;
    Url: string;
    Icon: string;
    ImageUrl: string;
    Uid: string;
    AssetTypeUid: string;
    Tags: SearchResultTags[];
}

export class SearchFullResult extends SearchResult {
    ID: number;
    Description: string;
    Group: string;
    Name: string;
    AbsoluteUrl: string;
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