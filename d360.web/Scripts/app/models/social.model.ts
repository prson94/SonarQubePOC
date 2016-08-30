export enum SocialVoteType {
    DownVote = -1,
    UpVote = 1
}

export class SocialCommentTag {
    Object: string;
    ObjectID: number;
    TextPath: string;
}

export class SocialVote {
    CommentID: number;
    ResourceID: number;
    Vote: SocialVoteType;
    ID: number;
}

export class SocialComment {
    Body: string;
    Comments: SocialComment[];
    CommentTypeID: number;
    CreatorIsOwner: boolean;
    CreatingResourceID: number;
    DateCreated: Date;
    DateCreatedUTCString: Date;
    DateEdited: Date;
    DateEditedUTCString: Date;
    ID: number;
    IsDeletable: boolean;
    IsDeleted: boolean;
    IsEditable: boolean;
    ObjectID: number;
    ObjectName: string;
    ObjectType: string;
    ResourceEmail: string;
    ResourceName: string;
    Tags: SocialCommentTag[];
    Votes: SocialVote[];
}

export class SocialEditCommentData{
    constructor(comment?: SocialComment, tags?: SocialCommentTag[])
    {
        if(comment)
            this.Comment = comment;

        if(tags)
            this.Tags = tags;
    }

    ObjectType: string;
    ObjectID: number;
    Comment: SocialComment;
    Tags: SocialCommentTag[];
}