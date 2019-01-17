import { Component} from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { AttributeTypeService } from '../../../services/attribute-type.service';
import { RightSidebarService } from '../../../services/right-sidebar.service';
import { MessagesService} from '../../../services/messages.service';
import { AdminBaseComponent } from '../admin-base.component';
import { AttributeType } from '../../../models/attribute-type.model';
import { TreeNode } from 'primeng/primeng';
import { Title } from '@angular/platform-browser';
import { type } from 'os';

@Component({
    selector: 'd3s-admin-attributes-component',  
    providers: [AttributeTypeService],
    template: ` <div class="row">
                    <div class="col l4 s12">                    
                        <div class="tile tile-detail">
                            <header *ngIf="!isLoading && !showDelete && !showEditor">Attribute Types
                                <d3s-tile-actions [hasAdd]="true" (addClick)="add()"></d3s-tile-actions>                            
                            </header>  
                            <d3s-loading [isLoading]="isLoading"></d3s-loading>
                            <span *ngIf="!isLoading && !showDelete && !showEditor">
                                <input type="text" pInputText [(ngModel)]="searchValue" placeholder="Search..." style="width: 100%;margin-bottom:10px;">                      
                                <p-treeTable [value]="attributes | treeSearch: searchValue:'Name'" selectionMode="single" [(selection)]="selected" dataKey="ID">
                                    <ng-template pTemplate="header">
                                        <tr>
                                            <th>
                                                ID
                                            </th>
                                            <th>
                                                Name
                                            </th>
                                            <th></th>
                                        </tr>
                                    </ng-template>
                                    <ng-template pTemplate="body" let-rowNode let-item="rowData">
                                        <tr [ttSelectableRow]="rowNode">
                                            <td>
                                                <d3s-treeTableToggler [rowNode]="rowNode"></d3s-treeTableToggler>
                                                {{item.ID}}
                                            </td>
                                            <td>
                                                {{item.Name}}
                                            </td>
                                            <td>
                                                <div class="RowTools">
                                                    <a style="cursor:pointer;" (click)="add(item.ID)"><i class="fa fa-plus"></i></a>
                                                    <a style="cursor:pointer;" (click)="selected=rowNode.node;showEditor=true"><i class="fa fa-pencil"></i></a>
                                                    <a style="cursor:pointer;" (click)="selected=rowNode.node;showDelete=true"><i class="fa fa-trash-o"></i></a>                                            
                                                </div>
                                            </td>
                                        </tr>
                                    </ng-template>
                                </p-treeTable>      
                            </span>
                            <d3s-delete-form *ngIf="showDelete"
                                [callback]="theDeleteCallback"
                                [itemId]="selected?.data?.ID"
                                [method]="'callback'"
                                [prompt]="'Are you sure you want to delete the attribute type [' + [selected?.data?.Name] + ']?'"                                         
                                (onCancel)="showDelete=false;"
                            ></d3s-delete-form>   
                            <d3s-admin-attribute-type-editor *ngIf="showEditor && !isLoading" [parentID]="parentID" [attribute]="selected?.data" (saveClick)="saveAttributeType($event)" (closeClick)="closeEditor()"></d3s-admin-attribute-type-editor>
                        </div>
                    </div>                    
                    <div class="col l8 s12" *ngIf="!showDelete && !showEditor">
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">                                              
                                    <d3s-field-definition-tile [objectType]="'AttributeType'" [objectID]="selected?.data?.ID"  [showIsListable]="false"></d3s-field-definition-tile>
                                </div>
                            </div>
                        </div>                        
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">                                              
                                    <d3s-admin-attribute-allocation [attributeID]="selected?.data?.ID"></d3s-admin-attribute-allocation>
                                </div>
                            </div>
                        </div>                        
                    <div>
                </div>  
                `
})

export class AdminAttributesComponent extends AdminBaseComponent {
    attributes: TreeNode[] = [];
    selected: TreeNode;

    showDelete: boolean = false;
    showEditor: boolean = false;
    theDeleteCallback: Function;
    parentID: number = 0;
    

    constructor(rightSidebarService: RightSidebarService, private attributeTypeService: AttributeTypeService, protected messagesService: MessagesService, headerBreadcrumbService: HeaderBreadcrumbService, titleService: Title) {
        super(headerBreadcrumbService, titleService, rightSidebarService);        
        this.areaName = "Attribute Groups";                
        this.setCommonItems();
        this.setCommonRightSideBar(true);
        if (this.auditSidebar) {
            this.auditSidebar.hasDynamicUrl = true;
            this.auditSidebar.dynamicUrlCallback = (() => {
                return `/sidebar/audit/AttributeType/${this.selected.data.ID}`
            });
        }
        this.theDeleteCallback = this.deleteAttributeType.bind(this);        
    }

    ngOnInit() {

        this.getAttributes();
    }

    ngOnDestroy() {        
        this.clearSidebar();
    }
    

    getAttributes() {
        this.isLoading = true;
        this.attributeTypeService.getAttributes()
            .then(result => {
                this.attributes = this.formTree(result)
                this.selected = this.attributes.length > 0 ? this.attributes[0] : null;            
                this.isLoading = false;
            });
    }

    private formTree(data): TreeNode[] {
        var tree = new Array<TreeNode>();

        data.filter(d => d.ParentID == null).forEach(d => {
            tree.push({ data: d, children: [] });
        });

        tree.forEach(t => {
            this.formTreeR(t, data);
        });
        
        return tree;
    }

    private formTreeR(node: TreeNode, data) {
        data.filter(d => d.ParentID == node.data.ID).forEach(d => {
            let child: TreeNode = { data: d, children: [] };
            node.children.push(child);
            this.formTreeR(child, data);
        });
    }


    deleteAttributeType(id: number) {
        this.attributeTypeService.deleteAttributeType(id).then(res => {
            this.showMessageForResult(this.messagesService, res);
            this.showDelete = false;
            this.selected = this.attributes.length > 0 ? this.attributes[0] : null;
            this.getAttributes();
        });
    }

    saveAttributeType(event) {
        this.isLoading = true;
        this.attributeTypeService.saveAttributeType(event.attribute)
            .then(result => {
                if (result.type == "error") {
                    this.isLoading = false;
                    this.messagesService.showError(result.title, result.message);
                } else {
                    this.getAttributes();      
                    this.isLoading = false;
                    this.showEditor = false;
                }
            });
    }

    closeEditor() {
        this.showEditor = false;
        if (this.selected == null) {
            this.selected = this.attributes.length > 0 ? this.attributes[0] : null;
        }
    }

    add(parentID: number) {
        this.showEditor = true;
        this.selected = null;
        this.parentID = parentID;
    }
}