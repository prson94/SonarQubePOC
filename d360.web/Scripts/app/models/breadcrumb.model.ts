import { TreeNode } from 'primeng/components/common/api';

export class Breadcrumb {
    text: string = "-";
    link: string = null;
    active: boolean;
    objectType: string;
    objectId: number;
    treeItems: TreeNode[];
    selectedTreeNode: TreeNode;
    isType: boolean;
    hasParent: boolean;

    constructor(text?: string, link?: string, active?: boolean, type?: string, objectId?: number, treeItems?: TreeNode[], selectedTreeNode?: TreeNode, isType?: boolean, hasParent?:boolean) {
        this.text = text === undefined ? "-" : text;
        this.link = link === undefined ? null : link;
        this.active = active === undefined ? false : active;
        this.objectType = type === undefined ? undefined : type;
        this.objectId = objectId === undefined ? undefined : objectId;
        this.treeItems = treeItems === undefined ? undefined : treeItems;
        this.selectedTreeNode = selectedTreeNode === undefined ? undefined : selectedTreeNode;
        this.isType = isType === undefined ? false : isType;
        this.hasParent = hasParent === undefined ? false : hasParent;
    }

    public hasLink(): boolean {
        return (this.link && this.link.length > 0 && this.active);
    }
}

export class BreadcrumbItem {
    Name: string;
    Url: string;
    Active: boolean;    
}