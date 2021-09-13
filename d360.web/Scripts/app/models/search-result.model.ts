export class SearchResultTags {
    Uid: string;
    Value: string;
    Highlight: string;
}

export class SearchPathComponent {
    Key: string[];
    AssetType: string;
}

export class SearchSelecton {
    ID: string;
    AssetUid: string;
    ObjectType: string;
    HasProfiling: boolean;
}

export class SearchResult {
    Name: string;
    DisplayName: string;
    Group: string;
    Type: string;
    Url: string;
    Icon: string;
    ImageUrl: string;
    AssetPath: SearchPathComponent[];
    Uid: string;
    AssetTypeUid: string;
    Tags: SearchResultTags[];
}

export class AssetScore {
    AssetUid: string;
    EffectiveDate: string;
    EndDate: string;
    Value: number;
    ScoreType: string;
    ShortName: string;
    RunDate: string;
    LowerThreshold: number;
    UpperThreshold: number;
}

export class SearchFullResult extends SearchResult {
    ID: string;
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
    Explanation: string;
    Fields: SearchResultFieldDisplay[];
    Status: string;
    Object: string;
    ObjectId: number;
    HasProfiling: boolean;
    Scores: AssetScore[];
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

export class SearchResultFieldDisplay {
    Name: string;
    Type: string;
    Label: string;
    Prefix: string;
    Suffix: string;
    Value: string;
    Empty: boolean;
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
    Explain: boolean;
    Force: boolean;
}

export class SearchCheckTreeVal {
    key: string;
    type: string;
    public constructor(k: string, t: string) {
        this.key = k;
        this.type = t;
    }
}

export class SearchState {
    Term: string;
    Size: number;
    From: number;
    SearchTypes: string[];
    CheckTreeKeys: SearchCheckTreeVal[];
    AdvancedFilters: AdvancedSearchFilter[];
    Querytime: Date;
    public constructor(init?: Partial<SearchState>) {
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

export class SearchAssetDetail {
    uid: string;
    Status: string;
    Path: string[][];
    DisplayValue: string;
    TypeName: string;
    Object: string;
    ObjectId: number;
    Id: number;
}

export class SearchDetail {
    AssetDetail: SearchAssetDetail;
    Scores: AssetScore[];
}
