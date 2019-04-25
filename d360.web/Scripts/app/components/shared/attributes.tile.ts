import {Input, Output, Component, OnInit} from '@angular/core';
import {FormMode, FormHelper} from '../../models/form.model';
import {AttributeHeirarchyItem, ToolbarItem} from '../../models/object-detail.model';
import {ObjectDetailService} from '../../services/object-detail.service';
import {TreeNode, MenuItem} from 'primeng/primeng';
import * as _ from 'lodash';


@Component({
    selector: 'd3s-attributes-tile',
    template: `
        <div *ngIf="isLoading">
            <div style="width:100%;text-align:center;"><i class="fa fa-spinner fa-spin"></i></div>
        </div>
        <div *ngIf="!isLoading">
            <div class="row">
                <div [class]="readonly ? 'col s12' : 'col s6'">
                    <p-treeTable [value]="items"
                                 selectionMode="single"
                                 [(selection)]="selectedRow"
                                 (onNodeSelect)="loadMenu();">
                        <ng-template pTemplate="header">
                            <tr>
                                <th></th>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="body"
                                     let-rowNode
                                     let-item="rowData">
                            <tr [ttSelectableRow]="rowNode">
                                <td>
                                    <d3s-treeTableToggler [rowNode]="rowNode"></d3s-treeTableToggler>
                                    <div *ngIf="item.IsCategory"
                                         class='Attribute-Category'
                                         style="display: inline-block">{{item.Name}}</div>
                                    <div *ngIf="!item.IsCategory"
                                         style="display: inline-block">
                                        <b *ngIf="item.ShowNameInTree">{{item.ObjectTypeName}}: </b>
                                        <span [innerHtml]="item.Name"></span>
                                    </div>
                                </td>
                            </tr>
                        </ng-template>
                    </p-treeTable>
                </div>
                <div *ngIf="!readonly"
                     class="col s6">
                    <div style="font-size: 1rem;">
                        <d3s-tile-actions hasMenu="true"
                                          [menuItems]="menuItems"
                                          (menuClick)="menuClick($event)"></d3s-tile-actions>
                    </div>

                    <div [ngSwitch]="formMode">
                        <div *ngSwitchDefault>
                            <object-detail *ngIf="detailType == 'Attribute'"
                                           [objectType]="detailType"
                                           [objectID]="detailID"></object-detail>
                        </div>
                        <div *ngSwitchCase="FormMode.Adding">
                            <d3s-dynamic-editor [selection]="null"
                                                [objectID]="0"
                                                [objectType]="'Attribute'"
                                                [title]="'Attribute'"
                                                [createUri]="'form/dynamicedit/create/attribute'"
                                                [createParams]="createParams"
                                                [editUri]="null"
                                                (closeClick)="formMode = FormMode.Default;"
                                                (saveClick)="formMode = FormMode.Default; load();"></d3s-dynamic-editor>
                        </div>
                        <div *ngSwitchCase="FormMode.Editing">
                            <d3s-dynamic-editor [selection]="selectedRowCopy"
                                                [objectID]="attributeID"
                                                [objectType]="'Attribute'"
                                                [title]="'Attribute'"
                                                [createUri]="null"
                                                [editUri]="'form/dynamicedit/edit/attribute'"
                                                (closeClick)="formMode = FormMode.Default;"
                                                (saveClick)="formMode = FormMode.Default; load();"></d3s-dynamic-editor>
                        </div>
                        <div *ngSwitchCase="FormMode.Deleting">
                            <d3s-delete-form
                                    [uri]="'form/DeleteAttributeByID?id=' + attributeID"
                                    [method]="'delete'"
                                    [prompt]="'Are you sure you want to remove this attribute?'"
                                    (onCancel)="formMode = FormMode.Default"
                                    (onDeleteSuccess)="formMode = FormMode.Default; load();">
                            </d3s-delete-form>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `,
    providers: [ObjectDetailService],
})

export class AttributesTile implements OnInit {
    @Input() objectType: string;
    @Input() objectID: number;
    @Input() readonly: boolean = true;
    @Output() itemCount: number = 0;

    @Input() hasAdd: boolean = true;
    @Input() hasEdit: boolean = true;
    @Input() hasDelete: boolean = true;

    private isLoading = false;
    private formMode = FormMode.Default;
    private FormMode = FormMode;

    private items: TreeNode[];
    private selectedRow: TreeNode;

    private detailType: string = null;
    private detailID: number = null;
    private detailUrl: string = '';
    private typeID: string = null;
    private createParams = [];
    private attributeID: number = null;
    private selectedRowCopy = null;

    private menuItems: MenuItem[] = [];
    private menuItemParams: MenuItemParams[] = [];

    constructor(private objectDetailService: ObjectDetailService) {
    }

    ngOnInit() {
        this.load();
    }

    load(): void {

        if (this.objectType == null || this.objectID == null)
            return;

        this.isLoading = true;

        this.objectDetailService.getAttributeHierarchyTree(this.objectID, this.objectType).subscribe(
            d => {
                this.items = d;
                this.itemCount = 0;
                this.items.forEach(i => this.itemCount += i.children.length);

                if (this.items.length > 0) {
                    this.selectedRow = this.items[0];
                    this.loadMenu();
                }

                this.isLoading = false;
            }
        );
    }

    add() {
        this.formMode = FormMode.Adding;
    }

    edit() {
        this.formMode = FormMode.Editing;
    }

    delete() {
        this.formMode = FormMode.Deleting;
    }

    loadMenu() {
        if (!this.selectedRow)
            return;

        this.formMode = FormMode.Default;

        let type = this.selectedRow.data.ObjectType;
        let id = this.selectedRow.data.ObjectID;
        let attributeID = null;
        let rootType = this.selectedRow.data.ParentObjectType;
        let rootID = this.selectedRow.data.ParentObjectID;
        let targetType = this.selectedRow.data.TargetObjectType;

        if (type === 'Attribute') {
            attributeID = id;
        }

        if (targetType) {
            this.detailType = targetType;
            this.detailID = this.selectedRow.data.TargetObjectID;
        } else {
            this.detailType = type;
            this.detailID = id;
        }

        this.objectDetailService.getAttributeActions(id, type, rootID, rootType, attributeID).subscribe(
            d => {
                this.setMenuItems(d);
            }
        );
    }

    setMenuItems(items: any[]) {

        this.menuItems = [];
        this.menuItemParams = [];

        items.forEach(i => {
            this.addMenuItem(i);
        });
    }

    addMenuItem(item: any) {
        let i = new MenuItemParams();
        i.action = item.Action;
        i.menuItem = {
            icon: 'fa fa-' + item.Icon,
            disabled: ((item.Action || '').toLowerCase() == 'add') ? false : (this.selectedRow == null),
        };
        i.params = item.Params;

        if (item.Items.length > 0) {
            i.menuItem.disabled = false;
            var counter = 0;
            item.Items.forEach(j => {
                let k = new MenuItemParams();
                k.action = j.Action;
                k.menuItem = {
                    icon: 'fa fa-' + j.Icon,
                    label: j.Title
                };
                k.params = j.Params;
                if (i.menuItem.items == undefined) {
                    i.menuItem.items = [];
                }
                i.menuItem.items[counter] = k.menuItem;
                counter++;
                this.menuItemParams.push(k);
            });
        }
        if ((item.Action != 'edit' && item.Action != 'delete' && item.Action != 'add') || (item.Action == 'edit' && this.hasEdit) || (item.Action == 'delete' && this.hasDelete) || (item.Action == 'add' && this.hasAdd)) {
            this.menuItemParams.push(i);
            this.menuItems.push(i.menuItem);
        }

    }

    menuClick(e: MenuItem) {
        let p = this.menuItemParams.find(m => m.menuItem == e);

        switch (p.action) {
            case 'add':
                this.createParams = [];
                this.createParams.push(p.params.typeID);
                this.createParams.push(this.objectType);
                this.createParams.push(this.objectID);
                this.createParams.push(p.params.parentID);
                this.add();
                break;
            case 'edit':
                this.attributeID = p.params.attributeID;
                this.selectedRowCopy = _.cloneDeep(this.selectedRow.data);
                this.selectedRowCopy.ID = this.selectedRowCopy.ID.split('|')[1];
                this.edit();
                break;
            case 'delete':
                this.attributeID = p.params.attributeID;
                this.delete();
                break;
        }
    }

}

export class MenuItemParams {
    menuItem: MenuItem;
    action: string;
    params: any;
}


