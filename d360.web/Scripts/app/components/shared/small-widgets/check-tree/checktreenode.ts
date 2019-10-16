export interface CheckTreeNode {
    label?: string;
    data?: any;
    count?: number;
    children?: CheckTreeNode[];
    leaf?: boolean;
    expanded?: boolean;
    type?: string;
    parent?: CheckTreeNode;
    partialSelected?: boolean;
    styleClass?: string;
    selectable?: boolean;
    key?: string;
}