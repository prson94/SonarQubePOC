///<reference path="../../es6-shim.d.ts"/>
import { Input, Output, Component, OnInit } from '@angular/core';
import { NgSwitch, NgSwitchDefault, NgSwitchCase } from '@angular/common';
import { FormMode, FormHelper } from '../../models/form.model';
import { AttributeHeirarchyItem, ToolbarItem } from '../../models/object-detail.model';
import { ObjectDetailService } from '../../services/object-detail.service';
import { TreeTable, TreeNode, Column, Menubar, MenuItem, Header } from 'primeng/primeng';
import { ObjectDetailTile } from './object-detail.tile';
import { DeleteForm } from '../forms/delete.form';
import { DynamicEditorComponent } from '../shared/dynamic-editor.component';


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
                            <b *ngIf="item.data.ShowNameInTree">{{item.data.ObjectTypeName}}: </b> <span [innerHtml]="item.data.Name"></span>
                        </div>
                    </template>
                </p-column>
            </p-treeTable>
        </div>
        <div *ngIf="!readonly" class="col l7 m7 s6">
            <p-menubar [model]="menuItems"></p-menubar>
            <div [ngSwitch]="formMode">
                <div *ngSwitchDefault>
                    <object-detail *ngIf="detailType == 'Attribute'" [objectType]="detailType" [objectID]="detailID"></object-detail>
                </div>
                <div *ngSwitchCase="FormMode.Adding">
                <d3s-dynamic-editor [selection]="null"
                                    [objectID]="0"
                                    [objectType]="'Attribute'"
                                    [title]="'Attribute'"
                                    [createUri]="'dynamiceditor/new/'+objectType+'/'+objectID+'/0/' + typeID"
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
    directives: [NgSwitch, NgSwitchCase, NgSwitchDefault, TreeTable, Column, Menubar, Header, ObjectDetailTile, DeleteForm, DynamicEditorComponent],
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
                if (this.items.length > 0) {
                    this.selectedRow = this.items[0];
                    this.loadMenu();
                }
                console.log(this.items);
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
        this.menuItems = [];
        if (!this.selectedRow)
            return;

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
                this.menuItems = FormHelper.convertToolBarToMenuItem(d);
                this.updateMenuItems(this.menuItems);
                console.log(this.menuItems);
            });
    }

    updateMenuItems(items: MenuItem[]) {
        for (var i = 0; i < items.length; i++) {

            //TODO: need to modify server side to pass this correctly when we finalize control library choice rather than this nonsense
            if (items[i].url) {
                if (items[i].url.toLowerCase().indexOf('addattribute') > -1) {
                    let startix = items[i].url.indexOf('typeID=') + 'typeID='.length;
                    let endix = items[i].url.indexOf('&objectType=');

                    this.typeID = items[i].url.substr(startix, endix - startix);
                    //console.log(this.typeID);
                    items[i].command = (e) => {
                        this.formMode = FormMode.Adding;
                        console.log(this.typeID);
                        

                    }
                } else if (items[i].url.toLowerCase().indexOf('editattribute') > -1) {
                    items[i].command = (e) => {
                        this.formMode = FormMode.Editing;
                    }
                }
            }

            items[i].url = null;

            if (items[i].items && items[i].items.length > 0)
                this.updateMenuItems(items[i].items);
        }
    }

}
