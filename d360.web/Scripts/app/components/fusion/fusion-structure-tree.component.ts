import { Input, Component, EventEmitter, Output, OnInit, OnChanges, SimpleChange } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FusionService } from '../../services/index';
import { BaseComponent } from '../shared/base.component';
import { FusionAttributeType, FusionConfigurationDetails  } from '../../models/fusion.model';
import { TreeNode } from 'primeng/primeng';

@Component({
    selector: 'd3s-fusion-structure-tree',
    template: ` 
                <div *ngIf="isLoading">
                    <div style="padding:10px;text-align:center;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                </div>
                <div class="tile tile-detail" *ngIf="!isLoading">
                    <header>Structure</header>
                    <input type="text" [(ngModel)]="searchValue" placeholder="Search..." style="width: 100%;"> 
                    <p-tree [value]="treeItems | breadcrumbTreeSearch: searchValue" selectionMode="single" [(selection)]="selected" [style]="{'line-height':'25px','width':'auto'}" 
                            (onNodeSelect)="nodeSelect($event)">                 
                    </p-tree>
                </div>
                `,
    providers: [FusionService],
})

export class FusionStructureTreeComponent extends BaseComponent implements OnChanges {

    @Input() fusion: FusionConfigurationDetails;

    @Input() fusionAttributeTypeId: number;
    @Output() fusionAttributeTypeIdChange = new EventEmitter();

    private treeItems: TreeNode[];
    private selected: TreeNode;

    private fusionAttributeTypes: FusionAttributeType[] = [];

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
            this.isLoading = false;
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
                    id: root.ID
                },
                children: (this.buildTreeNodeArray(attributes, root.ID)) //recursively find its children
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
        if (!event.node || !event.node.data || !event.node.data.id) {
            console.log("ERROR UNABLE TO DETERMINE SELECTED NODE'S ID.");

            return;
        }
        this.fusionAttributeTypeId = event.node.data.id
        this.fusionAttributeTypeIdChange.emit(this.fusionAttributeTypeId);
    }
};