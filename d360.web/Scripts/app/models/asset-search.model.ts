import { AssetTypeClass } from "./asset.model";

export class CommonComponentAssetTypeFilter {
    Uid: string;
    Class: AssetTypeClass;
    UseAsTransformation: boolean;
    AsSideOfRelationship: CommonComponentAssetTypeFilterSideOfRelationship;
}

export class CommonComponentAssetTypeFilterSideOfRelationship {
    PredicateType: string;
    PredicateUid: string;
    Side: CommonComponentAssetTypeFilterRelationshipSide;
}

export enum CommonComponentAssetTypeFilterRelationshipSide {
    Subject,
    Object
}

export class CommonComponentAssetResult {
    Uid: string;
    AssetTypeUid: string;
    Segments: CommonComponentAssetResultSegment[];
}

export class CommonComponentAssetResultSegment {
    Value: string;
}

export enum CommonComponentDisplayStyle {
    AbbreviatedPath,
    Name,
    Path
}

export enum CommonComponentSelectStyle {
    Button,
    CheckBox
}

export class AssetSearchFilter {
    SearchPhrase: string;
    PageSize: number;
    PageNum: number;
    Filters: CommonComponentAssetTypeFilter[];
}

export class AssetSearchApiResponse {
    items: CommonComponentAssetResult[];
    total: number;
    pageSize: number;
    pageNum: number;
}