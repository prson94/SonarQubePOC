import { EventEmitter } from '@angular/core';
import { TreeNode, MenuItem } from 'primeng/components/common/api';
import { ToolbarItem } from './object-detail.model';

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

    export function getSelectList(items: any[], label: string = 'label', value: string = 'value'): SelectItem[] {
        let list = new Array<SelectItem>();

        items.forEach(i => {
            let l = new SelectItem();
            l.label = i[label];
            l.Text = i[label];
            l.Value = i[value];
            l.value = i[value];

            list.push(l);
        });

        return list;

    }

    export function getDataUrl(file: File): Promise<any> {
        let reader = new FileReader();

        return new Promise<any>((resolve, reject) => {

            reader.onloadend = () => {
                resolve(reader.result);
            }
            reader.readAsDataURL(file);
        }).then(() => {
            return reader.result;
        });
    }

    export function formTree(data: any[], idField:string = 'ID', parentField:string = 'ParentID', expandAll: boolean = true): TreeNode[] {
        var tree = new Array<TreeNode>();
        if (data && data.filter) {
            data.filter(d => d[parentField] == null).forEach(d => {
                tree.push({ data: d, children: [], expanded: expandAll });
            });

            tree.forEach(t => {
                FormHelper.formTreeR(t, data, idField, parentField, expandAll);
            });
        }

        //console.log(tree);
        return tree;
    }

    export function formTreeR(node: TreeNode, data: any[], idField: string, parentField: string, expandAll: boolean = true) {
        data.filter(d => d[parentField] == node.data[idField]).forEach(d => {
            let child: TreeNode = { data: d, children: [], expanded: expandAll };
            node.children.push(child);
            FormHelper.formTreeR(child, data, idField, parentField, expandAll);
        });
     }

    export function flattenTree(data: any[], subDataField: string, idField: string = null, parentField: string = null): any[] {
        let flattened = [];
        for (var i = 0; i < data.length; i++) {
            flattened.push(data[i]);
            if (data[i][subDataField] && data[i][subDataField].length > 0) {
                let sub = flattenTree(data[i][subDataField], subDataField, idField, parentField);
                sub.forEach(s => {
                    if (idField && parentField)
                        s[parentField] = data[i][idField];
                    flattened.push(s)
                });
            }
        }
        return flattened;
    }

    export function convertToolBarToMenuItem(data: ToolbarItem[]): MenuItem[] {
        let items = [];
        for (var i = 0; i < data.length; i++) {
            let m: any = {};
            m.icon = 'fa-' + data[i].Icon;
            m.label = data[i].Title;
            m.url = data[i].Uri;

            if (data[i].Items.length > 0)
                m.items = convertToolBarToMenuItem(data[i].Items);

            items.push(m);
        }
        return items;
    }

    export function convertToNgUrl(data: any[], field: string) {
        for (let d of data) {
            d[field] = (d[field]).replace('#', 'a');
            d[field] = (d[field]).replace('artifacts', 'artifact');
        }
        return data;
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

