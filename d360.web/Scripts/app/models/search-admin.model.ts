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
	DatabaseCount: number;
    Start: string;
    LastUpdate: string;
	Menu: object[];
}

export class IndexPartialRebuild
{
	Class: number;
	AssetTypeUid: string;
}