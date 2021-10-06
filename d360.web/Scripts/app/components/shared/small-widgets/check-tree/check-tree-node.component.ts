import {
    Component, Input, OnInit, Inject, forwardRef
} from '@angular/core';
import { CheckTreeNode } from './checktreenode';
import { CheckTree } from './check-tree.component';

@Component({
    selector: 'd3s-check-treeNode',
    templateUrl: "./check-tree-node.component.html",
    styleUrls: ["check-tree-node.component.less"],
})
export class UICheckTreeNode implements OnInit {
    @Input() node: CheckTreeNode;
    @Input() parentNode: CheckTreeNode;
    @Input() root: boolean;
    @Input() index: number;
    @Input() firstChild: boolean;
    @Input() lastChild: boolean;
    @Input() level: number = 1;

    tree: CheckTree;

    constructor(@Inject(forwardRef(() => CheckTree)) tree) {
        this.tree = tree as CheckTree;
    }

    ngOnInit() {
        this.node.parent = this.parentNode;

        if (this.parentNode) {
            this.tree.syncNodeOption(this.node, this.tree.value, 'parent', this.tree.getNodeWithKey(this.parentNode.key, this.tree.value));
        }
    }

    isLeaf() {
        return this.tree.isNodeLeaf(this.node);
    }

    toggle(event: Event) {
        if (this.node.expanded)
            this.collapse(event);
        else
            this.expand(event);
    }

    expand(event: Event) {
        this.node.expanded = true;
        this.tree.onNodeExpand.emit({ originalEvent: event, node: this.node });
    }

    collapse(event: Event) {
        this.node.expanded = false;
        this.tree.onNodeCollapse.emit({ originalEvent: event, node: this.node });
    }

    onNodeClick(event: MouseEvent) {
        this.tree.onNodeClick(event, this.node);
    }

    onNodeTouchEnd() {
        this.tree.onNodeTouchEnd();
    }

    isSelected() {
        return this.tree.isSelected(this.node);
    }

    onKeyDown(event: KeyboardEvent) {
        const nodeElement = (<HTMLDivElement>event.target).parentElement.parentElement;

        if (nodeElement.nodeName !== 'D3S-CHECK-TREENODE') {
            return;
        }

        switch (event.which) {
            //down arrow
            case 40:
                const listElement = nodeElement.children[0].children[1];
                if (listElement && listElement.children.length > 0) {
                    this.focusNode(listElement.children[0]);
                }
                else {
                    const nextNodeElement = nodeElement.nextElementSibling;
                    if (nextNodeElement) {
                        this.focusNode(nextNodeElement);
                    }
                    else {
                        let nextSiblingAncestor = this.findNextSiblingOfAncestor(nodeElement);
                        if (nextSiblingAncestor) {
                            this.focusNode(nextSiblingAncestor);
                        }
                    }
                }

                event.preventDefault();
                break;

            //up arrow
            case 38:
                if (nodeElement.previousElementSibling) {
                    this.focusNode(this.findLastVisibleDescendant(nodeElement.previousElementSibling));
                }
                else {
                    let parentNodeElement = this.getParentNodeElement(nodeElement);
                    if (parentNodeElement) {
                        this.focusNode(parentNodeElement);
                    }
                }

                event.preventDefault();
                break;

            //right arrow
            case 39:
                if (!this.node.expanded) {
                    this.expand(event);
                }

                event.preventDefault();
                break;

            //left arrow
            case 37:
                if (this.node.expanded) {
                    this.collapse(event);
                }
                else {
                    let parentNodeElement = this.getParentNodeElement(nodeElement);
                    if (parentNodeElement) {
                        this.focusNode(parentNodeElement);
                    }
                }

                event.preventDefault();
                break;

            //space
            case 32:
            //enter
            case 13:
                this.tree.onNodeClick(event, this.node);
                event.preventDefault();
                break;

            default:
                //no op
                break;
        }
    }

    findNextSiblingOfAncestor(nodeElement) {
        let parentNodeElement = this.getParentNodeElement(nodeElement);
        if (parentNodeElement) {
            if (parentNodeElement.nextElementSibling)
                return parentNodeElement.nextElementSibling;
            else
                return this.findNextSiblingOfAncestor(parentNodeElement);
        }
        else {
            return null;
        }
    }

    findLastVisibleDescendant(nodeElement) {
        const childrenListElement = nodeElement.children[0].children[1];
        if (childrenListElement && childrenListElement.children.length > 0) {
            const lastChildElement = childrenListElement.children[childrenListElement.children.length - 1];

            return this.findLastVisibleDescendant(lastChildElement);
        }
        else {
            return nodeElement;
        }
    }

    getParentNodeElement(nodeElement) {
        const parentNodeElement = nodeElement.parentElement.parentElement.parentElement;

        return parentNodeElement.tagName === 'D3S-CHECK-TREENODE' ? parentNodeElement : null;
    }

    focusNode(element) {
        element.children[0].children[0].focus();
    }
}
