///<reference path="../../es6-shim.d.ts"/>
import { Component} from '@angular/core';
import { MessagesService, HeaderBreadcrumbService, PageHeader, AttributeTypeService, RightSidebarService  } from '../../services/index';
import { AdminBaseComponent } from './admin-base.component';
import { AttributeType } from '../../models/attribute-type.model';
import { TreeNode } from 'primeng/primeng';
import { Title } from '@angular/platform-browser';

@Component({
    selector: 'd3s-admin-attributes-component',  
    providers: [AttributeTypeService],
    template: ` <d3s-audit *ngIf="isAuditVisible" [objectID]="selected?.data?.ID" [objectName]="selected?.data?.Name" [objectType]="'AttributeType'"></d3s-audit>
                <div class="row" *ngIf="!isAuditVisible">
                    <div class="col l4 s12">                    
                        <div class="tile tile-detail">
                            <header *ngIf="!isLoading && !showDelete && !showEditor">Attribute Types
                                <d3s-tile-actions [hasAdd]="true" (addClick)="add()"></d3s-tile-actions>                            
                            </header>  
                            <div *ngIf="isLoading">
                                <div style="padding:10px;text-align:center;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                            </div>                                          
                            <p-treeTable *ngIf="!isLoading && !showDelete && !showEditor" [value]="attributes" selectionMode="single" [(selection)]="selected">
                                <p-column field="ID" header="ID"></p-column>
                                <p-column field="Name" header="Name"></p-column>
                                <p-column>
                                    <template let-col let-item="rowData" pTemplate type="body">
                                        <div class="RowTools">
                                            <a style="cursor:pointer;" (click)="add(item.data.ID)"><i class="fa fa-plus"></i></a>
                                            <a style="cursor:pointer;" (click)="selected=item;showEditor=true"><i class="fa fa-pencil"></i></a>
                                            <a style="cursor:pointer;" (click)="selected=item;showDelete=true"><i class="fa fa-trash-o"></i></a>                                            
                                        </div>
                                    </template>
                                </p-column>
                            </p-treeTable>      
                            <delete-form *ngIf="showDelete"
                                [callback]="theDeleteCallback"
                                [itemId]="selected?.data?.ID"
                                [method]="'callback'"
                                [prompt]="'Are you sure you want to delete the attribute type [' + [selected?.data?.Name] + ']?'"                                         
                                (onCancel)="showDelete=false;"
                            ></delete-form>   
                            <d3s-admin-attribute-type-editor *ngIf="showEditor && !isLoading" [parentID]="parentID" [attribute]="selected?.data" (saveClick)="saveAttributeType($event)" (closeClick)="closeEditor()"></d3s-admin-attribute-type-editor>
                        </div>
                    </div>                    
                    <div class="col l8 s12">
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">                                              
                                    <d3s-field-definition-tile [objectType]="'AttributeType'" [objectID]="selected?.data?.ID" ></d3s-field-definition-tile>
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
    

    constructor(rightSidebarService: RightSidebarService, private attributeTypeService: AttributeTypeService, protected messagesService: MessagesService, headerBreadcrumbService: HeaderBreadcrumbService, pageHeader: PageHeader, titleService: Title) {
        super(headerBreadcrumbService, pageHeader, titleService, rightSidebarService);
        this.areaDescription = "Here you will find all metadata that can be assigned to various objects and relationships.";
        this.areaName = "Attribute Groups";        
        //this.areaLink = window.location.pathname;
        this.setCommonItems();
        this.setCommonRightSideBar(true);
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
        this.attributeTypeService.deleteAttributeType(id);
        this.showDelete = false;
        this.selected = this.attributes.length > 0 ? this.attributes[0] : null;
        this.getAttributes();
    }

    saveAttributeType(event) {
        this.isLoading = true;
        this.attributeTypeService.saveAttributeType(event.attribute)
            .then(result => {
                this.getAttributes();      
                this.isLoading = false;
                this.showEditor = false;
            });
    }

    closeEditor() {
        this.showEditor = false;
        if (this.selected == null) {
            this.selected = this.attributes.length > 0 ? this.attributes[0] : null;
        }
    }

    add(parentID?: number) {
        this.showEditor = true;
        this.selected = null;
        this.parentID = parentID;
    }
}