import { Input, Component, EventEmitter, Output, OnInit, OnChanges, SimpleChange } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FusionService } from '../../services/index';
import { BaseComponent } from '../shared/base.component';
import { FusionAttributeType, FusionConfigurationDetails  } from '../../models/fusion.model';
import { TreeNode } from 'primeng/primeng';

@Component({
    selector: 'd3s-fusion-structure-tree',
    template: ` 
                <div class="tile tile-detail">
                    <header>Structure</header>
                    <input type="text" [(ngModel)]="searchValue" placeholder="Search..." style="width: 100%;"> 
                    <p-tree [value]="treeItems | breadcrumbTreeSearch: searchValue" selectionMode="single" [(selection)]="selected" [style]="{'line-height':'25px','width':'auto'}" 
                            (onNodeSelect)="nodeSelect($event)">                 
                    </p-tree>
                </div>
                `,
    providers: [FusionService],
})

export class FusionStructureTreeComponent extends BaseComponent implements OnInit {

    @Input() fusion: FusionConfigurationDetails;

    private treeItems: any[];
    private selected: any;

    private fusionAttributeTypes: FusionAttributeType[] = [];

    constructor(private fusionService: FusionService) {
        super();
    }

    ngOnInit() {
        
    }


    private load() {
        this.isLoading = true;
        this.fusionService.getFusionFusionAttributeTypes(this.fusion.FusionTypeID).then(res => {
            this.fusionAttributeTypes = res;
            this.treeItems = this.buildTreeNodeArray(this.fusionAttributeTypes);
            this.isLoading = false;
        });
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.fusion != null) {
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

    private nodeSelect(event) {

    }
};