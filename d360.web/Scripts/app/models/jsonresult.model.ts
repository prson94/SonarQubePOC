//{"type":"confirm","title":"Success!","action":"add","message":"test32 successfully created.","id":"50032","context":null,"custom":null}

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