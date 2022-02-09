export class SemanticTypeGetResponse {
    pageNum: number
    pageSize: number
    total: number;
    items: SemanticType[];
}

export class SemanticType {
    uid: string;
    createdBy: SemanticUserModel;
    updatedBy: SemanticUserModel;
    createdOn: Date;
    updatedOn: Date;
    effectiveDate: Date;
    source: SemanticSource;
    baseType: SemanticBaseType;
    description: string;
    //headerRegExps:
    headerRegExpConfidence: number;
    invalidList: string[];
    advanced: any;
    matchType: SemanticMatchType;
    maximum: number;
    minimum: number;
    minSamples: number;
    minMaxPresent: number;
    name: string;
    priority: number;
    qualifier: string;
    regExReturned: string;
    status: SemanticStatus;
    threshold: number;
    validLocales: string[];
    validList: string[];    
}

export class SemanticUserModel{
    id: number;
    uid: string;
    fullName: string;
}

export enum SemanticSource {
    BuiltIn = 1,
    UserDefined = 2
}

export enum SemanticBaseType {
    Boolean = 1,
    Double = 2,
    Long = 3,
    String = 4,
    LocalDate = 5,
    LocalTime = 6,
    LocalDateTime = 5,
    OffsetDateTime = 6,
    ZonedDateTime = 7
}

export enum SemanticMatchType {
    List = 1,    
    Pattern = 2,
    Number = 3,
    Advanced = 4
}

export enum SemanticStatus {
    Draft = 0,    
    InReview = 1,
    Certified = 2
}

export class SemanticTypeAsset {
    uid: string;
    path: string;
    assetTypePath: string;
    confidence: number;
}

export class SemanticTypeGetAssetsResponse {
    pageNum: number
    pageSize: number
    total: number;
    items: SemanticTypeAsset[];
}