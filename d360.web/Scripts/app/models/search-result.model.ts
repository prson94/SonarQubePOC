export class SearchResultTags {
    Uid: string;
    Value: string;
    Highlight: string;
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

export class SearchAggregationFilter {
    Field: string;
    Values: string[];
    public constructor(init?: Partial<SearchAggregationFilter>) {
        Object.assign(this, init);
    }}

export class SearchFieldFilter {
    Field: string;
    Phrase: string;
    MatchWords: boolean = false;
    public constructor(init?: Partial<SearchFieldFilter>) {
        Object.assign(this, init);
    }}

export class SearchQuery {
    Term: string;
    Size: number;
    From: number;
    AggregationFilters: SearchAggregationFilter[];
    FieldFilters: SearchFieldFilter[];
    Aggregations: string[];
    public constructor(init?: Partial<SearchQuery>) {
        Object.assign(this, init);
    }
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