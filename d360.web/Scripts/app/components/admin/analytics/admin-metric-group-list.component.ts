import { Input, Component, EventEmitter, Output } from '@angular/core';
import { MetricsService } from '../../../services/metrics.service';
import { Group } from '../../../models/metrics.model';
import { TreeNode } from 'primeng/primeng';
import { BaseComponent } from '../../shared/base.component';

@Component({
    selector: 'd3s-admin-metric-group-list',
    template: ` 
               <header>
                    Groups
                </header>
               <d3s-loading [isLoading]="isLoading"></d3s-loading>
               <div *ngIf="!isLoading">
                    <p-treeTable [value]="groupTree" [style]="{'width': '95', 'line-height' : '25px' }" selectionMode="single">
                        <p-column field="Name" header="Name"></p-column>
                        <p-column field="Description" header="Description"></p-column>
                    </p-treeTable>
                </div>
                `,
    providers: [MetricsService]
})

export class AdminMetricGroupListComponent extends BaseComponent {
    private groups: Group[] = [];
    private groupTree: TreeNode[] = [];

    constructor(private metricsService: MetricsService) {
        super();
    }

    ngOnInit() {
        this.isLoading = true;
        this.metricsService.getGroups()
            .then(r => {
                
                this.groups = r;
                this.groups.filter(g => g.ParentID == null).forEach(g => {
                    let n = {
                        data: g,
                        children: [],
                        expanded: true
                    }
                    this.groupTree.push(n);
                    this.addChildren(n);
                });
                this.isLoading = false;
            });
    }

    addChildren(node: TreeNode) {
        let children = this.groups.filter(g => g.ParentID == node.data.ID);
        if (children.length > 0) {
            children.forEach(c => {
                let n = {
                    data: c,
                    children: [],
                    expanded: true
                }
                node.children.push(n);
                this.addChildren(n);
            });
        }
    }

};