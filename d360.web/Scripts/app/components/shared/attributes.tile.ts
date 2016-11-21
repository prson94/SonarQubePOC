import { Input, Output, Component, OnInit } from '@angular/core';
import { FormMode, FormHelper } from '../../models/form.model';
import { AttributeHeirarchyItem, ToolbarItem } from '../../models/object-detail.model';
import { ObjectDetailService } from '../../services/object-detail.service';
import { TreeNode } from 'primeng/primeng';
import { MenuPartItem } from '../shared/menu.part';
import { ActionBarItem } from '../shared/action-bar.part'; 
import * as _ from 'lodash';


@Component({
    selector: 'd3s-attributes-tile',
    styles: [
        `
        .menu-bar-item {
            font-size:1.3em;
            padding:5px;
            cursor:pointer;
        }

        .menu-bar-item:hover {
            background-color:white;
        }

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
        `
    ],
    template: `
<div *ngIf="isLoading">
    <div style="width:100%;text-align:center;"><i class="fa fa-spinner fa-spin"></i></div>
</div>
<div *ngIf="!isLoading">
    <div class="row">
        <div [class]="readonly ? 'col s12' : 'col s6'">
            <p-treeTable [value]="items" selectionMode="single" [(selection)]="selectedRow" (onNodeSelect)="loadMenu();">
                <p-column>
                    <template let-item="rowData" pTemplate type="body">
                        <div *ngIf="item.data.IsCategory">
                            <span class='Attribute-Category'>{{item.data.Name}}</span>
                        </div>
                        <div *ngIf="!item.data.IsCategory">
                            <b *ngIf="item.data.ShowNameInTree">{{item.data.ObjectTypeName}}: </b> <span [innerHtml]="item.data.Name"></span>
                        </div>
                    </template>
                </p-column>
            </p-treeTable>
        </div>
        <div *ngIf="!readonly" class="col s6">
            <div style="float:right">
                <d3s-action-bar [items]="actions" (onClick)="action($event)" (onMenuClick)="menuAction($event)"></d3s-action-bar>
            </div>        
            
            <div [ngSwitch]="formMode">
                <div *ngSwitchDefault>
                    <object-detail *ngIf="detailType == 'Attribute'" [objectType]="detailType" [objectID]="detailID"></object-detail>
                </div>
                <div *ngSwitchCase="FormMode.Adding">
                <d3s-dynamic-editor [selection]="null"
                                    [objectID]="0"
                                    [objectType]="'Attribute'"
                                    [title]="'Attribute'"
                                    [createUri]="'dynamiceditor/new/' + objectType"
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
                                    [editUri]="'dynamiceditor/edit/' + objectType + '/' + attributeID"
                                    (closeClick)="formMode = FormMode.Default;"
                                    (saveClick)="formMode = FormMode.Default; load();"></d3s-dynamic-editor>
                </div> 
                <div *ngSwitchCase="FormMode.Deleting">
                    <delete-form
                        [uri]="'form/DeleteAttributeByID?id=' + attributeID"
                        [method]="'delete'"
                        [prompt]="'Are you sure you want to remove this attribute?'"
                        (onCancel)="formMode = FormMode.Default"
                        (onDeleteSuccess)="formMode = FormMode.Default">
                    </delete-form>
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

    private actions: ActionBarItem[] = new Array<ActionBarItem>();

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
                if (this.items.length > 0) {
                    this.selectedRow = this.items[0];
                    this.loadMenu();
                }                
                this.isLoading = false; 
            });
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

    save() {
        if (this.formMode == FormMode.Adding) {

        } else if (this.formMode == FormMode.Editing) {

        }
        this.formMode = FormMode.Default;
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

        this.objectDetailService.getAttributeActions(id, type, rootID, rootType, attributeID)
            .then(d => {
                this.setMenuItems(d);
            });
    }

    setMenuItems(items: any[]) {
        //console.log(items);
        this.actions = new Array<ActionBarItem>();

        let disable = (this.selectedRow == null);

        items.forEach(i => {
            let action = new ActionBarItem();
            action.icon = i.Icon;
            action.key = i.Action;
            action.title = i.Title;
            action.data = i.Params;
            action.disabled = ((action.key || '').toLowerCase() == 'add') ? false : disable;

            if (i.Items.length > 0) {
                action.disabled = false;
                action.menu = new Array<MenuPartItem>();
                i.Items.forEach(j => {
                    let sub = new MenuPartItem();
                    sub.icon = j.Icon;
                    sub.data = {
                        action: j.Action,
                        params: j.Params
                    };
                    sub.text = j.Title;

                    action.menu.push(sub);
                });
            }
            // only add permissible actions
            if ((i.Action != 'edit' && i.Action != 'delete' && i.Action != 'add') || (i.Action == 'edit' && this.hasEdit) || (i.Action == 'delete' && this.hasDelete) || (i.Action == 'add' && this.hasAdd))
                this.actions.push(action);
        });
    }

    action(item: ActionBarItem) {        
        switch ((item.key || '').toLowerCase().trim()) {
            case 'edit':
                this.attributeID = item.data.attributeID;
                this.selectedRowCopy = _.cloneDeep(this.selectedRow.data);
                this.selectedRowCopy.ID = this.selectedRowCopy.ID.split('|')[1];
                this.edit();
                break;
            case 'delete':
                this.attributeID = item.data.attributeID;
                this.delete();
                break;
            default:
                break;
        }
    }

    menuAction(item: MenuPartItem) {
        this.createParams = [];
        this.createParams = _.concat(
            item.data.params.typeID,
            item.data.params.objectType,
            item.data.params.typeID,
            item.data.params.parentID);

        if (item.data.action == 'add')
            this.add();
    }
}

export class MenuBarItem {
    icon: string;
    action: string;
    text: string;
    menuItems: MenuPartItem[] = new Array<MenuPartItem>();
    isMenu: boolean = false;
    params: any;
}


