
export class ApiResult {    
    ItemNumber: number;
    uid: string;
    ExecutionItemUid: string;
    Message: string;
    Success: boolean;
}

export class ErrorResponse {
    Title: string;
    Type: string;
    Message: string;
}