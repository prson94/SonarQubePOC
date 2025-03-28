export class SearchResultTags {
    Uid: string;
    Value: string;
    Highlight: string;
}

export class SearchPathComponent {
    Key: string[];
    AssetType: string;
}

export class SearchSelection {
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
	SemanticName: string;
	SemanticQualifier: string;
	SemanticUid: string;
}

export interface SearchAggregation {
	Uid: string;
	Class: string;
    Name: string;
    DisplayName: string;
    ResultCount: number;
    Items: SearchAggregation[];
}

export class SearchModel {
	Aggregations: SearchAggregation[];
	Total: number;
}

export class SearchResults {
    Results: SearchFullResult[];
	Aggregations: SearchAggregation[];
    Matches: number;
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

export class SearchAggregationFilter {
    Uid?: string;
	Class?: string;
}

export class SearchQuery {
    Term: string;
    Size: number;
	From: number;
	IncludeAggregations: boolean;
    AggregationFilters?: SearchAggregationFilter[];

	public constructor(init?: Partial<SearchQuery>) {
        Object.assign(this, init);
    }
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