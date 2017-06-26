import { Component, Input, ElementRef, ViewChildren, OnChanges, SimpleChange, Output, EventEmitter, Renderer, AfterViewInit, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { Router }       from '@angular/router';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { TypeaheadSearchService } from '../../../services/typeahead-search.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { ModelsService } from '../../../services/models.service';
import { SearchResult } from '../../../models/search-result.model';
import { TreeNode } from 'primeng/primeng';

@Component({
    selector: 'd3s-header-breadcrumb-item',
    providers: [TypeaheadSearchService, ModelsService],    
    host: {
        '(document:click)': 'onClick($event)',
        '(window:resize)': 'setMaxHeight()'
    },  
    template: ` <a *ngIf="breadcrumb.hasLink()" (click)="navigateToLink(breadcrumb.link)" class="breadcrumb" style="cursor:pointer">{{ breadcrumb.text }}</a>
                <div *ngIf="!breadcrumb.hasLink() && !showSearch" (mouseover)="in(treePanel,$event)" class="breadcrumb" [ngClass]="{'breadcrumb-link':isChangableItem() || isTreeItem()}">{{ breadcrumb.text }}</div>
                <p-autoComplete size="40"                                                      
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
                <div *ngIf="!lastItem && showSeperator" class="sep breadcrumb">::</div>                
                <p-overlayPanel #treePanel>  
                        <input type="text" pInputText [(ngModel)]="searchValue" placeholder="Search" style="width: 100%;">                      
                        <p-tree [value]="treeItems | treeSearch: searchValue" selectionMode="single" [(selection)]="breadcrumb.selectedTreeNode" styleClass="breadcrumbTree" [style]="{'max-height':maxOverlayHeight,'overflow':'auto','line-height':'25px'}" 
                            (onNodeSelect)="nodeSelect($event,treePanel)">
                            <ng-template let-node pTemplate type="default">
                                <span [ngStyle]="setTreeNodeStyles(node)">{{node.label}} <i *ngIf="node.data?.hasRelations" class="fa fa-share-alt" aria-hidden="true" title="Item has relationships" style="color:#999;"></i></span>
                            </ng-template>
                        </p-tree>
                </p-overlayPanel>                
              `,
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class HeaderBreadcrumbItemComponent implements OnChanges, OnInit {    
    @Input() breadcrumb: Breadcrumb;
    @Input() lastItem: boolean;
    @Output() treeClick = new EventEmitter();
    @Input() showSeperator: boolean = true;
    
    private results: SearchResult[];
    private result: SearchResult;
    private showSearch: boolean;
    private hasTree: boolean;
    private searchValue: string;
    private treeItems: TreeNode[] = [];
    private maxOverlayHeight: string = '800px'


    constructor(private renderer:Renderer, private modelsService: ModelsService, private elementRef: ElementRef, private router: Router,
                private typeaheadSearchService: TypeaheadSearchService) { }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.breadcrumb)
            this.treeItems = this.breadcrumb.treeItems;
    }

    ngOnInit() {
        this.setMaxHeight();
    }

    private setMaxHeight() {
        this.maxOverlayHeight = (window.innerHeight > 100) ? ((window.innerHeight - 120) + 'px') : '100px';
    }

    private isChangableItem() {
        return (this.breadcrumb.objectType && this.breadcrumb.objectId && !this.isTreeItem());
    }

    private isTreeItem(): boolean {
        return this.breadcrumb.objectType == 'Taxonomy' || this.breadcrumb.objectType == 'Policy';
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

    private navigateToLink(url: string) {
        this.router.navigateByUrl(url);
    }
    
}
