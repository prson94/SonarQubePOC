///<reference path="../../es6-shim.d.ts"/>
import { Input, Output, Component, OnInit } from '@angular/core';
import { NgSwitch, NgSwitchDefault, NgSwitchCase } from '@angular/common';
import { FormMode, FormHelper } from '../../models/form.model';
import { AttributeHeirarchyItem, ToolbarItem } from '../../models/object-detail.model';
import { ObjectDetailService } from '../../services/object-detail.service';
import { TreeTable, TreeNode, Column, Menubar, MenuItem } from 'primeng/primeng';


@Component({
    selector: 'd3s-attributes-tile',
    template: `
<div *ngIf="isLoading">
    <div style="width:100%;text-align:center;"><i class="fa fa-spinner fa-spin"></i></div>
</div>
<div *ngIf="!isLoading">
    <div class="row">
        <div class="col l5 m5 s6" [class]="readonly ? 'col s12' : 'col l5 m5 s6'">
            <p-treeTable [value]="items" selectionMode="single" [(selection)]="selectedRow" (onNodeSelect)="loadMenu();">
                <p-column>
                    <template let-item="rowData">
                        <div *ngIf="item.data.IsCategory">
                            <span class='Attribute-Category'>{{item.data.Name}}</span>
                        </div>
                        <div *ngIf="!item.data.IsCategory">
                            <b *ngIf="item.data.ShowNameInTree">{{item.data.ObjectTypeName}}: </b> {{item.data.Name}}
                        </div>
                    </template>
                </p-column>
            </p-treeTable>
        </div>
        <div *ngIf="!readonly" class="col l7 m7 s6">
            <p-menubar [model]="menuItems"></p-menubar>
            <div [ngSwitch]="formMode">
                <div *ngSwitchDefault>
                    default
                </div>
            </div>
        </div>
    </div>

</div>
`,
    directives: [NgSwitch, NgSwitchCase, NgSwitchDefault, TreeTable, Column, Menubar],
    providers: [ObjectDetailService],
})

export class AttributesTile implements OnInit {
    @Input() objectType: string;
    @Input() objectID: number;
    @Input() readonly: boolean = true;
    @Output() itemCount: number = 0;

    private isLoading = false;
    private formMode = FormMode.Default;
    private FormMode = FormMode;

    private items: TreeNode[];
    private selectedRow: TreeNode;

    private menuItems: MenuItem[];

    constructor(private objectDetailService: ObjectDetailService) {
    }

    ngOnInit() {
        this.load();
    }


    load(): void {

        if (this.objectType == null || this.objectID == null)
            return;

        this.isLoading = true;

        this.objectDetailService.getAttributeHierarchyTree(this.objectID, this.objectType)
            .then(d => {
                this.items = d;
                this.itemCount = 0;
                this.items.forEach(i => this.itemCount += i.children.length);
                //console.log(this.items);
                this.isLoading = false; 
            });
    }

    add() {
        this.formMode = FormMode.Default;
    }

    edit() {
        this.formMode = FormMode.Default;
    }

    delete() {
        this.formMode = FormMode.Default;
    }

    save() {
        if (this.formMode == FormMode.Adding) {

        } else if (this.formMode == FormMode.Editing) {

        }
        this.formMode = FormMode.Default;
    }

    loadMenu() {
        this.menuItems = [];
        if (!this.selectedRow)
            return;

        let type = this.selectedRow.data.ObjectType;
        let id = this.selectedRow.data.ObjectID;
        let detailType, detailID, attributeID = null;
        let rootType = this.selectedRow.data.ParentObjectType;
        let rootID = this.selectedRow.data.ParentObjectID;
        let targetType = this.selectedRow.data.TargetObjectType;

        if (type === 'Attribute') {
            attributeID = id;
        }

        if (targetType) {
            detailType = targetType;
            detailID = this.selectedRow.data.TargetObjectID;
        } else {
            detailType = type;
            detailID = id;
        }

        this.objectDetailService.getAttributeActions(id, type, rootID, rootType, attributeID).
            then(d => {
                //console.log('tools: ');
                //console.log(d);

                this.menuItems = FormHelper.convertToolBarToMenuItem(d);
                console.log(this.menuItems);
            });
    }
}
