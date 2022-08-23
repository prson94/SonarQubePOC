export class FollowDetail {
    ResourceID: number;
    FollowID: number;
    ID: number;
    Name: string;
    TextPath: string;
    Description: string;
    ParentID: number;
    ParentType: string;
    Url: string;
    TypeID: number;
    Type: string;
    TypeName: string;
    IconBackColor: string;
    IconForeColor: string;
    IconText: string;
    OpenEventCount: number;
    CurrentScore: number;
    FollowerEmail: string;
    FollowerFirstName: string;
    FollowerLastName: string;
    FollowerName: string;
    FollowerObjectType: string;
    FollowerObjectID: number;
    FollowerUrl: string;
	HardFollow: boolean;
	AssetID: number;
	AssetTypeID: number;
}


export class Follow {
    ID: number;
    ResourceID: number;
    DateCreate: string;
	FollowTypeID: FollowType;
	AssetID: number;
	AssetTypeID: number;
}

export class FollowInfo {
    isFollowing: boolean;
    isFollowingParent: boolean;
    parent: Follow;
}

export enum FollowType {
    Single = 1,
    Parent = 3,
    Child = 5
}