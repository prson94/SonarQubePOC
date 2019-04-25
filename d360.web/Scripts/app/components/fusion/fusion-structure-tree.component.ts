import {Component, EventEmitter, Input, OnChanges, Output, SimpleChange} from '@angular/core';
import {TreeNode} from 'primeng/primeng';
import {takeUntil} from "rxjs/operators";
import {Subject} from "rxjs";

import {FusionAttributeType, FusionConfigurationDetails, FusionQueryAttributeType} from '../../models/fusion.model';

import {FusionService} from '../../services/fusion.service';

import {BaseComponent} from '../shared/base.component';

@Component({
    selector: 'd3s-fusion-structure-tree',
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <span *ngIf="!isLoading">
                <input type="text" [(ngModel)]="searchValue" placeholder="Search..." style="width: 100%;"/> 
                <p-tree [value]="treeItems | treeSearch: searchValue" selectionMode="single" [(selection)]="selected"
                        [style]="{'line-height':'25px','width':'auto'}"
                        (onNodeSelect)="nodeSelect($event)">                 
                </p-tree>
               </span>`,
    providers: [FusionService],
})

export class FusionStructureTreeComponent extends BaseComponent implements OnChanges {
    @Input() fusion: FusionConfigurationDetails;

    @Input() fusionAttributeTypeId: number;
    @Input() fusionQueryAttributeTypeId: number;

    @Output() fusionQueryAttributeTypeIdChange = new EventEmitter();
    @Output() fusionAttributeTypeIdChange = new EventEmitter();

    @Input() showFusionQueryConfig: boolean;
    @Output() showFusionQueryConfigChange = new EventEmitter();

    @Output() loaded = new EventEmitter();

    private treeItems: TreeNode[];
    private selected: TreeNode;

    public fusionAttributeTypes: FusionAttributeType[] = [];
    public fusionQueryAttributeTypes: FusionQueryAttributeType[] = [];

    destroySubject$: Subject<void> = new Subject();

    constructor(private fusionService: FusionService) {
        super();
    }

    public load() {
        this.isLoading = true;

        this.fusionService
            .getFusionFusionAttributeTypes(this.fusion.FusionTypeID)
            .pipe(takeUntil(this.destroySubject$))
            .subscribe(
                res => {
                    this.fusionAttributeTypes = res;

                    this.treeItems = this.buildTreeNodeArray(this.fusionAttributeTypes);

                    this.fusionService
                        .getFusionQueryAttributeTypes(this.fusion.FusionTypeID, this.fusion.ID)
                        .pipe(takeUntil(this.destroySubject$))
                        .subscribe(
                            res => {
                                this.fusionQueryAttributeTypes = res;

                                var queriesNode = {
                                    label: 'Queries',
                                    expanded: true,
                                    data: {
                                        type: 'FusionQueryAttributeType',
                                        id: -1
                                    },
                                    children: (this.buildQueryTreeNodeArray(this.fusionQueryAttributeTypes)) //recursively find its children
                                };

                                this.treeItems.push(queriesNode);

                                // handle initial selected item
                                if (this.fusionAttributeTypeId) {
                                    this.selected = this.findSelectedTreeNode(this.fusionAttributeTypeId, 'FusionAttributeType');
                                    this.fusionAttributeTypeIdChange.emit(this.fusionAttributeTypeId);
                                } else if (this.fusionQueryAttributeTypeId) {
                                    this.selected = this.findSelectedTreeNode(this.fusionQueryAttributeTypeId, 'FusionQueryAttributeType');
                                    this.fusionQueryAttributeTypeIdChange.emit(this.fusionQueryAttributeTypeId);
                                } else if (this.showFusionQueryConfig) {
                                    this.selected = this.findSelectedTreeNode(-1, 'FusionQueryAttributeType');
                                    this.showFusionQueryConfigChange.emit(this.showFusionQueryConfig);
                                } else if (this.treeItems.length > 0) {
                                    this.fusionAttributeTypeId = this.treeItems[0].data.id;
                                    if (!this.fusionQueryAttributeTypeId) {
                                        this.selected = this.findSelectedTreeNode(this.fusionAttributeTypeId, 'FusionAttributeType');
                                    }
                                    if (this.fusionAttributeTypeId > 0) {
                                        this.fusionAttributeTypeIdChange.emit(this.fusionAttributeTypeId);
                                    }
                                }

                                this.loaded.emit();

                                this.isLoading = false;
                            }
                        );
                });
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes['fusion'] && this.fusion != null) {
            this.load();
        }
    }

    private buildTreeNodeArray(attributes: FusionAttributeType[], Parent?: number): TreeNode[] {
        //find the root items then 
        let rootNodes = attributes.filter(x => (Parent != undefined ? x.ParentID == Parent : !x.ParentID));
        let res: TreeNode[] = [];

        if (rootNodes.length == 0) {
            return [];
        }

        for (let root of rootNodes) {
            res.push({
                label: root.Name,
                expanded: true,
                data: {
                    type: 'FusionAttributeType',
                    id: root.ID
                },
                children: (this.buildTreeNodeArray(attributes, root.ID)) //recursively find its children
            });
        }

        return res;
    }

    private buildQueryTreeNodeArray(attributes: FusionQueryAttributeType[]): TreeNode[] {
        //find the root items then
        let res: TreeNode[] = [];

        for (let qry of attributes) {
            res.push({
                label: qry.Name,
                expanded: true,
                data: {
                    type: 'FusionQueryAttributeType',
                    id: qry.ID
                },
                children: null
            });
        }

        return res;
    }

    private findSelectedTreeNode(
        id: number,
        type: string
    ): TreeNode {
        let nodes: TreeNode[] = [];
        let node = nodes[0];

        // add root nodes
        for (let rNode of this.treeItems) {
            nodes.push(rNode);
        }

        //do a breadth first search for the given treenode
        if (nodes.length == 0) {
            return;
        }

        while (node) {
            if (node.data.id && node.data.id == id && node.data.type == type) {
                return node;
            }

            //push children
            if (node.children) {
                for (let cNode of node.children) {
                    nodes.push(cNode);
                }
            }

            //remove this node
            nodes.splice(0, 1);

            if (nodes.length == 0) {
                return null;
            }

            node = nodes[0];
        }
    }

    private nodeSelect(event) {
        if (!event.node || !event.node.data || !event.node.data.id) {
            console.log("ERROR UNABLE TO DETERMINE SELECTED NODE'S ID.");
            return;
        }

        this.showFusionQueryConfig = false;

        if (event.node.data.type == "FusionAttributeType") {
            this.fusionAttributeTypeId = event.node.data.id;
            this.fusionAttributeTypeIdChange.emit(this.fusionAttributeTypeId);
        } else {
            if (event.node.data.id == -1) {
                this.showFusionQueryConfig = true;
            } else {
                this.fusionQueryAttributeTypeId = event.node.data.id;
                this.fusionQueryAttributeTypeIdChange.emit(this.fusionQueryAttributeTypeId);
            }
        }

        this.showFusionQueryConfigChange.emit(this.showFusionQueryConfig);
    }
}
