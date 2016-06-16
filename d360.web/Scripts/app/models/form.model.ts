export class SelectItem {
    Disabled: boolean;
    Group: string;
    Selected: boolean;
    Text: string;
    Value: string;
    label: string;
    value: string;
}

export module FormHelper {
    export function mapSelectItems(s: SelectItem[]) {
        s.forEach(s => { s.value = s.Value; s.label = s.Text });
    }
}


export enum MessageType {
    Error,
    Success,
    Info,
    Warning
}

export class FormMessage {
    MessageType: MessageType;
    Message: string;
    Visible: boolean = true;

    public Success(msg: string): void {
        this.MessageType = MessageType.Success;
        this.Message = msg;
    }
    public Info(msg: string): void {
        this.MessageType = MessageType.Info;
        this.Message = msg;
    }
    public Error(msg: string): void {
        this.MessageType = MessageType.Error;
        this.Message = msg;
    }

    public Warning(msg: string): void {
        this.MessageType = MessageType.Warning;
        this.Message = msg;
    }

    get isError(): boolean {
        return this.MessageType == MessageType.Error;
    }

    get isSuccess(): boolean {
        return this.MessageType == MessageType.Success;
    }

    get isInfo(): boolean {
        return this.MessageType == MessageType.Info;
    }

    get isWarning(): boolean {
        return this.MessageType == MessageType.Warning;
    }

}

export class JsonResult {
    type: string;
    title: string;
    message: string;
    action: string;
    id: string;
    statusCode: string;
    context: string;
    customdata: any;

    constructor(data: any) {
        this.type = data.type || null;
        this.title = data.title || null;
        this.message = data.message || null;
        this.action = data.action || null;
        this.id = data.id || null;
        this.statusCode = data.statusCode || null;
        this.context = data.context || null;
        this.customdata = data.customdata || null;

    }

    get isError(): boolean {
        return ((this.type || '').toLowerCase().trim() == 'error');
    }

    get isSuccess(): boolean {
        return ((this.type || '').toLowerCase().trim() == 'confirm' || (this.type || '').toLowerCase().trim() == 'success');
    }
}