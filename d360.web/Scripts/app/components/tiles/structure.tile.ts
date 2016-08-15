///<reference path="../../es6-shim.d.ts"/>
import { Input, Output, Component, OnChanges, SimpleChange } from '@angular/core';
import { HierarchyModel, PredicateType } from '../../models/relations.model';
import { ObjectDetailService } from '../../services/object-detail.service';
import { TreeTable, TreeNode, Header, Column } from 'primeng/primeng';


@Component({
    selector: 'd3s-structure-tile',
    template: `
                <div *ngIf="isLoading">
                    <div style="width:100%;text-align:center;"><i class="fa fa-spinner fa-spin"></i></div>
                </div>
                <div *ngIf="!isLoading">
                    <p-treeTable [value]="items" selectionMode="single" [(selection)]="selectedRow">
                        <p-column>
                            <template let-item="rowData">
                                    <div style="font-size:14px;font-weight:600;">
                                        {{item.data.Name}}&nbsp;&nbsp;<span style="font-size:.7em;font-weight:normal;">{{item.data.ObjectTypeName}}</span>
                                        <!-- 
                                            <span style="font-size:.7em;font-weight:normal; padding: 2px 5px 2px 5px; background-color: darkgray; color:white; border-radius: 5px;">{{item.data.ObjectTypeName}}</span>
                                        -->
                                    </div>
                            </template>
                        </p-column>
                    </p-treeTable>
                </div>
                `,
    providers: [ObjectDetailService],
    directives: [TreeTable, Header, Column]
})

export class StructureTile implements OnChanges {
    @Input() objectType: string;
    @Input() objectID: number;
    @Input() readonly: boolean;

    private isLoading = false;

    items: TreeNode[];
    selectedRow: TreeNode;

    constructor(private objectDetailService: ObjectDetailService) {
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            //if (p == 'objectType') {
            //    this.objectType = changes['objectType'].currentValue;
            //}
            //if (p == 'objectID') {
            //    this.objectID = changes['objectID'].currentValue;
            //}
        }

        this.load();
    }

    load(): void {

        if (this.objectType == null || this.objectID == null)
            return;

        this.isLoading = true;
        this.objectDetailService.getRelationsHierarchyTree(PredicateType.TypeHierarchy, this.objectType, this.objectID)
            .then(d => {
                this.items = d;
                this.isLoading = false;
                console.log(this.items);
            });

    }
}
