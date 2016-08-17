///<reference path="../../es6-shim.d.ts"/>
import { Input, Output, Component, OnChanges, SimpleChange } from '@angular/core';
import { HierarchyModel, PredicateType } from '../../models/relations.model';
import { ObjectDetailService } from '../../services/object-detail.service';
import { TreeTable, TreeNode, Header, Column, Tooltip } from 'primeng/primeng';
import { RelationshipsService } from '../../services/relationships.service';


@Component({
    selector: 'd3s-structure-tile',
    styles: [
        `
        .menu-bar {
            background-color:#ccc;
            padding: 2px;
        }

        .menu-item {
            cursor: pointer;
            padding:5px 10px 5px 10px;
            border:1px solid #aaa;
            display: inline-block;   
            background-color: #ddd;
            transition: all .5s;     
        }

        .menu-item:hover {
            background-color: #fff;
        }

        .menu-item.disabled:hover {
            background-color: #ddd;
        }

        .menu-item.disabled {
            cursor: default;
        }

        .row-item {
            font-size:14px;
            font-weight:600;
        }

        .item-type {
            font-size:.7em;
            font-weight:normal;
        }

        `
    ],
    template: `
                <div *ngIf="isLoading">
                    <div style="width:100%;text-align:center;"><i class="fa fa-spinner fa-spin"></i></div>
                </div>
                <div *ngIf="!isLoading">
                    <div class="row">
                        <div class="col s12 m6">
                            <p-treeTable [value]="items" selectionMode="single" [(selection)]="selectedRow">
                                <p-column>
                                    <template let-item="rowData">
                                            <div class="row-item">
                                                <span [style.color]="((item.data.Level > 0) ? (item.data.ObjectID == objectID && item.data.Object == objectType) : (item.data.SubjectID == objectID && item.data.Subject == objectType)) ? '#00C' : '#000'" >{{item.data.Name}}</span>&nbsp;&nbsp;<span class="item-type">{{item.data.ObjectTypeName}}</span>
                                            </div>
                                    </template>
                                </p-column>
                            </p-treeTable>
                        </div>
                        <div class="col s12 m6">
                            <div class="menu-bar">
                                <div (click)="action('parent')" class="menu-item" [class.disabled]="selectedRow == null" pTooltip="add a parent" tooltipPosition="top"><i class="fa fa-level-up fa-flip-horizontal"></i></div>
                                <div (click)="action('child')" class="menu-item" [class.disabled]="selectedRow == null" pTooltip="add a child" tooltipPosition="top"><i class="fa fa-level-down"></i></div>
                                <div (click)="action('edit')" class="menu-item" [class.disabled]="selectedRow == null" pTooltip="edit selected artifact" tooltipPosition="top"><i class="fa fa-pencil"></i></div>
                                <div (click)="action('delete')" class="menu-item" [class.disabled]="selectedRow == null" pTooltip="delete selected artifact" tooltipPosition="top"><i class="fa fa-trash-o"></i></div>
                            </div>
                        </div>
                    </div>
                </div>
                `,
    providers: [ObjectDetailService, RelationshipsService],
    directives: [TreeTable, Header, Column, Tooltip]
})

export class StructureTile implements OnChanges {
    @Input() objectType: string;
    @Input() objectID: number;
    @Input() readonly: boolean;

    private isLoading = false;
    private hasChanges = false;
    items: TreeNode[];
    selectedRow: TreeNode;

    constructor(private objectDetailService: ObjectDetailService, private relationshipService: RelationshipsService) {
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
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
                //console.log(this.items);
            });
    }

    action(action: string) {
        switch (action) {
            case 'delete':
                this.isLoading = true;
                this.relationshipService.deleteHierarchyItem(this.selectedRow.data.ID)
                    .then(() => this.load());
                break;
        }
    }

}
