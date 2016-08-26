export class SocialCommentTag {
    Object: string;
    ObjectID: number;
    TextPath: string;
}

export class SocialComment {
    Body: string;
    Comments: string;
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
    Votes: any[];
}