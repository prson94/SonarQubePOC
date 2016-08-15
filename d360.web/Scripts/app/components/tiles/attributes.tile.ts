///<reference path="../../es6-shim.d.ts"/>
import { Input, Output, Component, OnInit } from '@angular/core';
import { NgSwitch, NgSwitchDefault, NgSwitchCase } from '@angular/common';
import { FormMode, FormHelper } from '../../models/form.model';
import { AttributeHeirarchyItem, ToolbarItem } from '../../models/object-detail.model';
import { ObjectDetailService } from '../../services/object-detail.service';
import { TreeTable, TreeNode, Column, Header } from 'primeng/primeng';
import { ObjectDetailTile } from './object-detail.tile';
import { DeleteForm } from '../forms/delete.form';
import { DynamicEditorComponent } from '../shared/dynamic-editor.component';
import { MenuPart, MenuPartItem } from '../parts/menu.part';


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
        `
    ],
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
                            <b *ngIf="item.data.ShowNameInTree">{{item.data.ObjectTypeName}}: </b> <span [innerHtml]="item.data.Name"></span>
                        </div>
                    </template>
                </p-column>
            </p-treeTable>
        </div>
        <div *ngIf="!readonly" class="col l7 m7 s6">
            <div style="background-color:#ccc; padding:5px;">          
                <div *ngFor="let item of menuBarItems" style="display: inline;">
                    <d3s-menu *ngIf="item.isMenu" [items]="item.menuItems" (onItemClick)="menuClick($event)"><span style="font-size:1.3em;"><i [class]="'fa fa-' + item.icon"></i></span></d3s-menu>
                    <span *ngIf="!item.isMenu" class="menu-bar-item" (click)="barClick(item)"><i [class]="'fa fa-' + item.icon"></i></span>
                </div>
            </div>
            
            <div [ngSwitch]="formMode">
                <div *ngSwitchDefault>
                    <object-detail *ngIf="detailType == 'Attribute'" [objectType]="detailType" [objectID]="detailID"></object-detail>
                </div>
                <div *ngSwitchCase="FormMode.Adding">
                adding
                <d3s-dynamic-editor [selection]="null"
                                    [objectID]="0"
                                    [objectType]="'Attribute'"
                                    [title]="'Attribute'"
                                    [createUri]="'dynamiceditor/new/' + objectType + '/' + objectID + '/0/' + typeID"
                                    [editUri]="null"
                                    (closeClick)="formMode = FormMode.Default;"
                                    (saveClick)="formMode = FormMode.Default; load();"></d3s-dynamic-editor>
                </div>
                <div *ngSwitchCase="FormMode.Editing">
                    editing
                </div>
                <div *ngSwitchCase="FormMode.Deleting">
                    deleting
                </div>
            </div>
        </div>
    </div>
</div>
`,
    directives: [NgSwitch, NgSwitchCase, NgSwitchDefault, TreeTable, Column, Header, ObjectDetailTile, DeleteForm, DynamicEditorComponent, MenuPart],
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

    private detailType: string = null;
    private detailID: number = null;
    private detailUrl: string = '';
    private typeID: string = null;

   // private menuItems: MenuItem[];
   // private menuPartItems: MenuPartItem[] = new Array<MenuPartItem>();
    private menuBarItems: MenuBarItem[] = new Array<MenuBarItem>();

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
                //console.log(this.items);
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

        this.objectDetailService.getAttributeActions(id, type, rootID, rootType, attributeID).
            then(d => {
                //console.log(d);
                this.setMenuItems(d);

            });
    }

    setMenuItems(items: any[]) {

        this.menuBarItems = new Array<MenuBarItem>();

        for (let i = 0; i < items.length; i++) {
            let item = items[i];
            let barItem = new MenuBarItem();
            barItem.icon = item.Icon;
            barItem.uri = item.Uri;
            if (item.Items.length > 0) {
                barItem.isMenu = true;
                for (let j = 0; j < item.Items.length; j++) {
                    let subItem = item.Items[j];
                    let menuItem = new MenuPartItem();

                    menuItem.icon = item.icon;
                    menuItem.text = subItem.Title;
                    menuItem.data = subItem.Uri;

                    barItem.menuItems.push(menuItem);
                }
            }
            this.menuBarItems.push(barItem);
        }        
    }

    menuClick(item: MenuPartItem) {
        this.add();
        console.log(item);
    }

    barClick(item: MenuBarItem) {
        if (item.uri.toLowerCase().indexOf('editattribute')) {
            this.edit();
        } else if (item.uri.toLowerCase().indexOf('deleteattribute')) {
            this.delete();
        }
        console.log(item);
    }

}

export class MenuBarItem {
    icon: string;
    uri: string;
    menuItems: MenuPartItem[] = new Array<MenuPartItem>();
    isMenu: boolean = false;
}


