import { TreeNode } from 'primeng/components/common/api';
import { RENDERER } from '@angular/core/src/render3/interfaces/view';

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
    parentTypeName: string;
    parentType: string;
    parentID: number;
    parentUrl: string;

    constructor(text?: string,
                link?: string,
                active?: boolean,
                type?: string,
                objectId?: number,
                treeItems?: TreeNode[],
                selectedTreeNode?: TreeNode,
                isType?: boolean,
                hasParent?: boolean,
                parentTypeName?: string,
                parentType?: string,
                parentID?: number,
                parentURL?: string) {
        this.text = text === undefined ? "-" : text;
        this.link = link === undefined ? null : link;
        this.active = active === undefined ? false : active;
        this.objectType = type === undefined ? undefined : type;
        this.objectId = objectId === undefined ? undefined : objectId;
        this.treeItems = treeItems === undefined ? undefined : treeItems;
        this.selectedTreeNode = selectedTreeNode === undefined ? undefined : selectedTreeNode;
        this.isType = isType === undefined ? false : isType;
        this.parentTypeName = parentTypeName === undefined ? undefined : parentTypeName;
        this.hasParent = hasParent === undefined ? false : hasParent;
        this.parentType = parentType === undefined ? undefined : parentType;
        this.parentID = parentID === undefined ? undefined : parentID;
        this.parentUrl = parentURL === undefined ? undefined : parentURL;
    }

    public hasLink(): boolean {
        return (this.link && this.link.length > 0 && this.active);
    }
}

export class BreadcrumbItem {
    Name: string;
    Url: string;
    TypeName: string;
    TypeUrl: string;
}
