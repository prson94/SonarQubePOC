import { EventEmitter } from '@angular/core';
import { TreeNode } from 'primeng/primeng';

export class BaseEditorModel {
    FormUri: string;
    FormMethod: string;
    FormName: string;
    FormDescription: string;
    IsUsed: boolean;
}

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

    export function getDataUrl(file: File): Promise<string> {
        let reader = new FileReader();

        return new Promise<string>((resolve, reject) => {

            reader.onloadend = () => {
                resolve(reader.result);
            }
            reader.readAsDataURL(file);
        }).then(() => {
            //console.log(reader.result);
            return reader.result;
        });
    }


     export function formTree(data: any[], idField:string = 'ID', parentField:string = 'ParentID'): TreeNode[] {
        var tree = new Array<TreeNode>();

        data.filter(d => d[parentField] == null).forEach(d => {
            tree.push({ data: d, children: [] });
        });

        tree.forEach(t => {
            FormHelper.formTreeR(t, data, idField, parentField);
        });
        //console.log(tree);
        return tree;
    }

    export function formTreeR(node: TreeNode, data: any[], idField: string, parentField: string) {
        data.filter(d => d[parentField] == node.data[idField]).forEach(d => {
            let child: TreeNode = { data: d, children: [] };
            node.children.push(child);
            FormHelper.formTreeR(child, data, idField, parentField);
        });
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

export enum FormMode {
    Default = 1,
    Editing = 2,
    Adding = 3,
    Deleting = 4,
}

export interface FormEvents {
    ///when the form action is canceled
    onCancel: EventEmitter<any>;
    ///when the form actions completes, regardless of success/fail
    onComplete: EventEmitter<any>; 
    ///when the form action completes successfully
    onSuccess: EventEmitter<any>;
    ///when the form action fails
    onError: EventEmitter<any>; 
    ///when the form has loaded
    onLoadComplete: EventEmitter<any>;
}

