export class ObjectStatisticChildItem {
    Count: number;
    Name: string;
    TypeID: number;
}

export class ObjectStatistics {
    CommentCount: number;
    CommentLast: string;
    FollowerCount: number;
    IssueCount: number;
    IssueLast: string;
    Score: number;
    ScoreLast: string;
    Items: ObjectStatisticChildItem[];
}