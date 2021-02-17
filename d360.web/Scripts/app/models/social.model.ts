export enum Emoji {
    ThumbsUp = 1,
    ThumbsDown = 2
}

export enum CommentType {
    System = 1,
    Social = 2,
    Issue = 5
}

export class CommentRelationDetail {
    AssetUid: string;
    Path: string;
    TypeName: string;
    Url: string;
    IconBackColor: string;
    IconForeColor: string;
}

export class CommentAggregateVoteDetail {
    Count: number;
    Emoji: Emoji;
}

export class CommentDetail {
    Uid: string;
    Body: string;
    AssetUid: string;
    CommentType: CommentType;
    IsDeleted: boolean;

    ID: number;
    ParentID: number;
    CreatedBy: number;
    CreatedByUid: string;
    UpdatedBy: number;
    CreatedOn: Date;
    UpdatedOn: Date;

    ResourceName: string;
    AssetPath: string;
    Url: string;

    IsDeletable: boolean;
    IsEditable: boolean;

    Comments: CommentDetail[];
    Tags: CommentRelationDetail[];
    Emojis: CommentAggregateVoteDetail[];
}

export class CommentDetails {
    count: number;
    page: number;
    pageSize: number;
    comments: CommentDetail[];
}

export class CommentVoteDetail {
    emoji: Emoji;
    resourceUid: string;
    userDisplayName: string;
}

export class CommentApiPostModel {
    AssetUid: string;
    ParentUid: string;
    Body: string;
    /*
     *A list of unique identifiers for the list os assets you are tagging to this comment. 
     */
    Tags: string[];
}

export class CommentApiPutModel {
    Uid: string;
    Body: string;
    /*
     *A list of unique identifiers for the list os assets you are tagging to this comment. 
     */
    Tags: string[];
}