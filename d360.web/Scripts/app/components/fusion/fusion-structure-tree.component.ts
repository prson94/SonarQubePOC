import { Input, Component, EventEmitter, Output, OnInit, OnChanges, SimpleChange } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FusionService } from '../../services/index';
import { BaseComponent } from '../shared/base.component';
import { FusionAttributeType, FusionQueryAttributeType, FusionConfigurationDetails  } from '../../models/fusion.model';
import { TreeNode } from 'primeng/primeng';

@Component({
    selector: 'd3s-fusion-structure-tree',
    template: `<d3s-loading [isLoading]="isLoading"></d3s-loading>
               <span *ngIf="!isLoading">
                <input type="text" [(ngModel)]="searchValue" placeholder="Search..." style="width: 100%;"/> 
                <p-tree [value]="treeItems | treeSearch: searchValue" selectionMode="single" [(selection)]="selected" [style]="{'line-height':'25px','width':'auto'}" 
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

    private treeItems: TreeNode[];
    private selected: TreeNode;

    public fusionAttributeTypes: FusionAttributeType[] = [];
    public fusionQueryAttributeTypes: FusionQueryAttributeType[] = [];

    constructor(private fusionService: FusionService) {
        super();
    }

    
    private load() {
        this.isLoading = true;
        this.fusionService.getFusionFusionAttributeTypes(this.fusion.FusionTypeID).then(res => {
            this.fusionAttributeTypes = res;
            this.treeItems = this.buildTreeNodeArray(this.fusionAttributeTypes);
            if (this.fusionAttributeTypeId) this.selected = this.findSelectedTreeNode(this.fusionAttributeTypeId);
            else if (this.treeItems.length > 0){
                this.fusionAttributeTypeId = this.treeItems[0].data.id;
                this.selected = this.findSelectedTreeNode(this.fusionAttributeTypeId);
                this.fusionAttributeTypeIdChange.emit(this.fusionAttributeTypeId);
            }

            this.fusionService.getFusionQueryAttributeTypes(this.fusion.FusionTypeID, this.fusion.ID).then(res => {
                this.fusionQueryAttributeTypes = res;

                var queriesNode = {
                    label: 'Queries',
                    expanded: true,
                    data: {
                        type: 'FusionQueryAttributeType',
                        id: 0
                    },
                    children: (this.buildQueryTreeNodeArray(this.fusionQueryAttributeTypes)) //recursively find its children
                };

                this.treeItems.push(queriesNode);
                this.isLoading = false;
            });
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

        if (rootNodes.length == 0) return null;

        let res: TreeNode[] = [];

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

    private findSelectedTreeNode(id: number): TreeNode {
        let nodes: TreeNode[] = [];

        // add root nodes
        for (let rNode of this.treeItems) {
            nodes.push(rNode);
        }

        //do a breadth first search for the given treenode
        if (nodes.length == 0) return;

        let node = nodes[0];

        while (node) {
            if (node.data.id && node.data.id == id) return node;

            //push children
            if (node.children) {
                for (let cNode of node.children) {
                    nodes.push(cNode);
                }
            }

            //remove this node
            nodes.splice(0, 1);

            if (nodes.length == 0) return null;
            node = nodes[0];
        }
    }

    private nodeSelect(event) {
        //console.log(event.node.data.type + ' ' + event.node.data.id);
        if (!event.node || !event.node.data || !event.node.data.id) {
            console.log("ERROR UNABLE TO DETERMINE SELECTED NODE'S ID.");
            return;
        }

        if (event.node.data.type == "FusionAttributeType") {
            this.fusionAttributeTypeId = event.node.data.id;
            this.fusionAttributeTypeIdChange.emit(this.fusionAttributeTypeId);
        }
        else {
            this.fusionQueryAttributeTypeId = event.node.data.id;
            this.fusionQueryAttributeTypeIdChange.emit(this.fusionQueryAttributeTypeId);
        }
    }
};