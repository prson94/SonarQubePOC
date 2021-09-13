import {
    Component, Input, AfterContentInit, OnDestroy, Output, EventEmitter, OnInit, OnChanges,
    ContentChildren, QueryList, TemplateRef, ElementRef
} from '@angular/core';
import { Optional } from '@angular/core';
import { CheckTreeNode } from './checktreenode';
import { PrimeTemplate } from 'primeng/api';
import { TreeDragDropService } from 'primeng/api';
import { BlockableUI } from 'primeng/api/blockableui';
import { ObjectUtils } from 'primeng/utils';

@Component({
    selector: 'd3s-check-tree',
    templateUrl: "./check-tree.component.html",
    styleUrls: ["check-tree.component.less"],
})
export class CheckTree implements OnInit, OnChanges, AfterContentInit, OnDestroy, BlockableUI {
    @Input() value: CheckTreeNode[];
    @Input() selection: any;

    @Output() selectionChange: EventEmitter<any> = new EventEmitter();
    @Output() onNodeSelect: EventEmitter<any> = new EventEmitter();
    @Output() onNodeUnselect: EventEmitter<any> = new EventEmitter();
    @Output() onNodeExpand: EventEmitter<any> = new EventEmitter();
    @Output() onNodeCollapse: EventEmitter<any> = new EventEmitter();

    @Input() style: any;
    @Input() styleClass: string;
    @Input() metaKeySelection: boolean = true;
    @Input() propagateSelectionUp: boolean = true;
    @Input() propagateSelectionDown: boolean = true;

    @Input() loading: boolean;
    @Input() loadingIcon: string = 'pi pi-spinner';
    @Input() emptyMessage: string = 'No records found';

    @Input() title: string;
    @Input() ariaLabel: string;
    @Input() ariaLabelledBy: string;

    @Input() filter: boolean;
    @Input() filterBy: string = 'label';
    @Input() filterMode: string = 'lenient';
    @Input() filterPlaceholder: string;

    @Input() nodeTrackBy: Function = (index: number, item: any) => item;

    @ContentChildren(PrimeTemplate) templates: QueryList<any>;

    public templateMap: any;
    public nodeTouched: boolean;
    public filteredNodes: CheckTreeNode[];
    private timeoutId: number;

    constructor(public el: ElementRef, @Optional() public dragDropService: TreeDragDropService) { }

    ngOnInit() {

    }

    ngOnChanges(changes: any) {
        if (changes['value'] !== undefined) {
            window.setTimeout(() => {
                this.checkPropagation();
            }, 50);
        }
    }

    ngAfterContentInit() {
        if (this.templates.length) {
            this.templateMap = {};
        }

        this.templates.forEach((item) => {
            this.templateMap[item.name] = item.template;
        });
    }

    get styles(): string {
        let styles: string[] = ["check-tree-container"];
        if (this.styleClass) {
            styles.push(this.styleClass);
        }
        return styles.join(" ");
    }

    onNodeClick(event, node: CheckTreeNode) {
        let eventTarget = (<Element>event.target);

        if (eventTarget.className && eventTarget.className.indexOf('tree-toggler') === 0) {
            return;
        }
        else {
            if (node.selectable === false) {
                return;
            }

            if (this.hasFilteredNodes()) {
                node = this.getNodeWithKey(node.key, this.value);

                if (!node) {
                    return;
                }
            }

            let index = this.findIndexInSelection(node);
            let selected = (index >= 0);

            if (selected) {
                if (this.propagateSelectionDown)
                    this.propagateDown(node, false);
                else
                    this.selection = this.selection.filter((val, i) => i != index);

                if (this.propagateSelectionUp && node.parent) {
                    this.propagateUp(node.parent, false);
                }

                this.selectionChange.emit(this.selection);
                this.onNodeUnselect.emit({ originalEvent: event, node: node });
            }
            else {
                if (this.propagateSelectionDown)
                    this.propagateDown(node, true);
                else
                    this.selection = [...this.selection || [], node];

                if (this.propagateSelectionUp && node.parent) {
                    this.propagateUp(node.parent, true);
                }

                this.selectionChange.emit(this.selection);
                this.onNodeSelect.emit({ originalEvent: event, node: node });
            }
        }

        this.nodeTouched = false;
    }

    onNodeTouchEnd() {
        this.nodeTouched = true;
    }

    findIndexInSelection(node: CheckTreeNode) {
        let index: number = -1;

        if (this.selection) {
            for (let i = 0; i < this.selection.length; i++) {
                let selectedNode = this.selection[i];
                let areNodesEqual = (selectedNode.key && selectedNode.key === node.key) || selectedNode == node;
                if (areNodesEqual) {
                    index = i;
                    break;
                }
            }
        }

        return index;
    }

    syncNodeOption(node, parentNodes, option, value?: any) {
        // to synchronize the node option between the filtered nodes and the original nodes(this.value) 
        const _node = this.hasFilteredNodes() ? this.getNodeWithKey(node.key, parentNodes) : null;
        if (_node) {
            _node[option] = value || node[option];
        }
    }

    hasFilteredNodes() {
        return this.filter && this.filteredNodes && this.filteredNodes.length;
    }

    getNodeWithKey(key: string, nodes: CheckTreeNode[]): CheckTreeNode {
        for (let node of nodes) {
            if (node.key === key) {
                return node;
            }

            if (node.children) {
                let matchedNode = this.getNodeWithKey(key, node.children);
                if (matchedNode) {
                    if (matchedNode.parent == undefined)
                        matchedNode.parent = node;
                    return matchedNode;
                }
            }
        }
    }

    propagateUp(node: CheckTreeNode, select: boolean) {
        if (node.children && node.children.length) {
            let selectedCount: number = 0;
            let childPartialSelected: boolean = false;
            for (let child of node.children) {
                if (this.isSelected(child)) {
                    selectedCount++;
                }
                else if (child.partialSelected) {
                    childPartialSelected = true;
                }
            }

            if (select && selectedCount == node.children.length) {
                this.selection = [...this.selection || [], node];
                node.partialSelected = false;
            }
            else {
                if (!select) {
                    let index = this.findIndexInSelection(node);
                    if (index >= 0) {
                        this.selection = this.selection.filter((val, i) => i != index);
                    }
                }

                if (childPartialSelected || selectedCount > 0 && selectedCount != node.children.length)
                    node.partialSelected = true;
                else
                    node.partialSelected = false;
            }

            this.syncNodeOption(node, this.filteredNodes, 'partialSelected');
        }

        let parent = node.parent;
        if (parent) {
            this.propagateUp(parent, select);
        }
    }

    propagateDown(node: CheckTreeNode, select: boolean) {
        let index = this.findIndexInSelection(node);

        if (select && index == -1) {
            this.selection = [...this.selection || [], node];
        }
        else if (!select && index > -1) {
            this.selection = this.selection.filter((val, i) => i != index);
        }

        node.partialSelected = false;

        this.syncNodeOption(node, this.filteredNodes, 'partialSelected');

        if (node.children && node.children.length) {
            for (let child of node.children) {
                this.propagateDown(child, select);
            }
        }
    }

    isSelected(node: CheckTreeNode) {
        return this.findIndexInSelection(node) != -1;
    }

    isNodeLeaf(node) {
        return node.leaf == false ? false : !(node.children && node.children.length);
    }

    getRootNode() {
        return this.filteredNodes ? this.filteredNodes : this.value;
    }

    getTemplateForNode(node: CheckTreeNode): TemplateRef<any> {
        if (this.templateMap)
            return node.type ? this.templateMap[node.type] : this.templateMap['default'];
        else
            return null;
    }

    onFilter(event) {
        let filterValue = event.target.value;
        if (filterValue === '') {
            this.filteredNodes = null;
        }
        else {
            this.filteredNodes = [];
            const searchFields: string[] = this.filterBy.split(',');
            const filterText = ObjectUtils.removeAccents(filterValue).toLowerCase();
            const isStrictMode = this.filterMode === 'strict';
            for (let node of this.value) {
                let copyNode = { ...node };
                let paramsWithoutNode = { searchFields, filterText, isStrictMode };
                if ((isStrictMode && (this.findFilteredNodes(copyNode, paramsWithoutNode) || this.isFilterMatched(copyNode, paramsWithoutNode))) ||
                    (!isStrictMode && (this.isFilterMatched(copyNode, paramsWithoutNode) || this.findFilteredNodes(copyNode, paramsWithoutNode)))) {
                    this.filteredNodes.push(copyNode);
                }
            }
        }
    }

    findFilteredNodes(node, paramsWithoutNode) {
        if (node) {
            let matched = false;
            if (node.children) {
                let childNodes = [...node.children];
                node.children = [];
                for (let childNode of childNodes) {
                    let copyChildNode = { ...childNode };
                    if (this.isFilterMatched(copyChildNode, paramsWithoutNode)) {
                        matched = true;
                        node.children.push(copyChildNode);
                    }
                }
            }

            if (matched) {
                node.expanded = true;
                return true;
            }
        }
    }

    isFilterMatched(node, { searchFields, filterText, isStrictMode }) {
        let matched = false;
        for (let field of searchFields) {
            let fieldValue = ObjectUtils.removeAccents(String(ObjectUtils.resolveFieldData(node, field))).toLowerCase();
            if (fieldValue.indexOf(filterText) > -1) {
                matched = true;
            }
        }

        if (!matched || (isStrictMode && !this.isNodeLeaf(node))) {
            matched = this.findFilteredNodes(node, { searchFields, filterText, isStrictMode }) || matched;
        }

        return matched;
    }

    showClearSelection() {
        return (this.selection && this.selection.length > 0);
    }

    clearSelection() {
        this.selection = [];
        this.selectionChange.emit(this.selection);
    }

    public expandAll() {
        let nodes = this.getRootNode();
        nodes.forEach((n) => {
            this.expandCollapse(n, true);
        });
    }

    public collapseAll() {
        let nodes = this.getRootNode();
        nodes.forEach((n) => {
            this.expandCollapse(n, false);
        });
    }

    private expandCollapse(node: CheckTreeNode, expand: boolean) {
        node.expanded = expand;
        node.children?.forEach((c) => this.expandCollapse(c, expand));
    }

    private checkPropagation() {
        for (let i = 0; i < this.selection.length; i++) {
            let node = this.getNodeWithKey(this.selection[i].key, this.value);
            if (node) {
                this.propagateDown(node, true);
                node.expanded = true;
                if (node.parent) {
                    let parent = node.parent;
                    parent.expanded = true;
                    if (this.findIndexInSelection(parent) == -1) {
                        this.propagateUp(parent, true);
                    }
                }
            }
        }
    }

    getBlockableElement(): HTMLElement {
        return this.el.nativeElement.children[0];
    }

    ngOnDestroy() {
        window.clearTimeout(this.timeoutId);
    }
}
