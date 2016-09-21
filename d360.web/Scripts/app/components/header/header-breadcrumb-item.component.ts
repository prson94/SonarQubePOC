///<reference path="../../es6-shim.d.ts"/>
import { Component, Input, ElementRef, ViewChildren, OnChanges, SimpleChange, Output, EventEmitter, Renderer, AfterViewInit } from '@angular/core';
import { Router }       from '@angular/router';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { TypeaheadSearchService, HeaderBreadcrumbService, ModelsService } from '../../services/index';
import { SearchResult } from '../../models/search-result.model';
import { TreeNode } from 'primeng/primeng';

@Component({
    selector: 'd3s-header-breadcrumb-item',
    providers: [TypeaheadSearchService, ModelsService],    
    host: {
        '(document:click)': 'onClick($event)',
    },
    styles: [`
    a.breadcrumb {
        color:#54a4da;
    }
    .breadcrumb {
        font-weight:bold;
        text-transform:uppercase;
    }
    .link {
        border-bottom: 1px solid #3979a2;
        cursor:pointer;
    }           
  `],
    template: ` <a *ngIf="breadcrumb.hasLink()" [routerLink]="[breadcrumb.link]" class="breadcrumb">{{ breadcrumb.text }}</a>
                <span *ngIf="!breadcrumb.hasLink() && !showSearch" (mouseover)="in(treePanel,$event)" class="breadcrumb" [ngClass]="{'link':isChangableItem() || isTreeItem()}">{{ breadcrumb.text }}</span>
                <p-autoComplete size="50"                                                      
                            *ngIf="showSearch" 
                            [inputStyle]="{'border':'2px solid #54a4da','border-radius':'4px'}"
                            styleClass="searchTypeahead"             
                            [minLength]="1"                               
                            [(ngModel)]="result" 
                            [suggestions]="results" 
                            (completeMethod)="search($event)" 
                            field="Name"  
                            [placeholder]="breadcrumb.text"
                            (onSelect)="selectItem()">                       
                    </p-autoComplete>                    
                <span *ngIf="!lastItem" class="sep breadcrumb"> :: </span>                
                <p-overlayPanel #treePanel>  
                        <input type="text" [(ngModel)]="searchValue" placeholder="Search" style="width: 100%;">                      
                        <p-tree [value]="treeItems | breadcrumbTreeSearch: searchValue" selectionMode="single" [(selection)]="breadcrumb.selectedTreeNode" styleClass="breadcrumbTree" [style]="{'max-height':'800px','overflow':'auto','line-height':'25px'}" 
                            (onNodeSelect)="nodeSelect($event,treePanel)">
                            <template let-node pTemplate type="default">
                                <span [ngStyle]="setTreeNodeStyles(node)">{{node.label}} <i *ngIf="node.data?.hasRelations" class="fa fa-share-alt" aria-hidden="true" title="Item has relationships" style="color:#999;"></i></span>
                            </template>
                        </p-tree>
                </p-overlayPanel>                
              `
})

export class HeaderBreadcrumbItemComponent implements OnChanges {    
    @Input() breadcrumb: Breadcrumb;
    @Input() lastItem: boolean;
    @Output() treeClick = new EventEmitter();
    
    private results: SearchResult[];
    private result: SearchResult;
    private showSearch: boolean;
    private hasTree: boolean;
    private searchValue: string;
    private treeItems: TreeNode[] = [];

    constructor(private renderer:Renderer, private modelsService: ModelsService, private elementRef: ElementRef, private router: Router,
                private typeaheadSearchService: TypeaheadSearchService) { }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.breadcrumb)
            this.treeItems = this.breadcrumb.treeItems;
    }

    ngAfterViewInit() {
        
    }

    private isChangableItem() {
        return (this.breadcrumb.objectType && this.breadcrumb.objectId && !this.isTreeItem());
    }

    private isTreeItem(): boolean {
        return this.breadcrumb.objectType == 'Taxonomy';
    }
    
    private in(panel, event) {
        if (this.isChangableItem()) {
            this.showSearch = true;
        }        
        if (this.isTreeItem()) {
            panel.toggle(event);            
        }
    }    

    search(event) {
        this.typeaheadSearchService.getObjectTypeItems(10, event.query, this.breadcrumb.objectType, this.breadcrumb.objectId).then(data => {
            this.results = data;
        });
    }

    selectItem() {
        this.router.navigateByUrl(this.result.Url);
    }

    onClick(event) {
        if (this.showSearch && !this.elementRef.nativeElement.contains(event.target)) { 
            this.showSearch = false;            
        }
    }
    
    nodeSelect(event, panel) {        
        this.breadcrumb.text = event.node.label;
        this.treeClick.emit({ id: event.node.data.id });      
        panel.hide();
    }

    setTreeNodeStyles(node) {
        if (!node.data) return null;

        let styles = {            
            'font-weight': node.data.hasRelations ? 'bold' : 'normal',            
        };
        return styles;
    }

}
