export class IndexableType {
    Name: string;
    Class: number;
    ClassName: string;
    AssetTypeUid: string;
    }

export class IndexableStatus extends IndexableType
{
    Status: number;
    TargetCount: number;
    CurrentCount: number;
    Start: string;
    LastUpdate: string;
}

export class IndexPartialRebuild
{
	Class: number;
	AssetTypeUid: string;
}