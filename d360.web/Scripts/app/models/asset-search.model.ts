import { AssetTypeClass } from "./asset.model";
import { Predicate } from "./predicate.model";

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
    AssetTypeIcon: string;
    AssetTypeName: string;
    Segments: CommonComponentAssetResultSegment[];
}

export class CommonComponentAssetResultExt extends CommonComponentAssetResult {
    IsSelected: boolean;
}

export class CommonComponentAssetSelection {
    Uid: string;
    AssetTypeUid: string;
    AssetTypeIcon: string;
    AssetTypeName: string;
    Segments: CommonComponentAssetResultSegment[];
    Predicate: Predicate;
    Warnings: string[] = [];
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
    Button = 'Button',
    CheckBox = 'CheckBox'
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

export class V2ApiFilters {
    _pageSize: number;
    _pageNum: number;
    _order: string;
    _direction: string;
    _predicateUid: string;
    _subjectUid: string;
    _objectUid: string;
    _assetUid: string;
    _simpleFilter: string;
    _ownedBy: string;
    _filter: string;
    _relationFilter: string;
    useTypeLevelDefaultSorts: boolean;
    _loadPermissionDetails: boolean;
    _includeParent: boolean;
    _excludeCount: boolean;
    usegraphforparent: boolean;
    _onlyListableFields: boolean;
    _includeOwnershipLookup: boolean;
    _listColorsAsJSON: boolean;
    _isHierachyItem: boolean;
    _includeProfilingCheck: boolean;
}
