
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

export class ConfirmResponse {
	title: string;
	type: string;
	message: string;
}