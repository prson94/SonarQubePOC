
import {debounceTime} from 'rxjs/operators';
import { Component, Input, ElementRef, ViewChildren, OnChanges, SimpleChange, Output, EventEmitter, AfterViewInit, OnInit,OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { Router }       from '@angular/router';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { TypeaheadSearchService } from '../../../services/typeahead-search.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SearchResult } from '../../../models/search-result.model';
import { TreeNode } from 'primeng/components/common/api';
import { SubscriptionLike as ISubscription } from 'rxjs';

@Component({
    selector: 'd3s-header-breadcrumb-item',
    providers: [TypeaheadSearchService],    
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
                            [autoHighlight]="true"
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

export class HeaderBreadcrumbItemComponent implements OnChanges, OnInit, OnDestroy {    
    @Input() breadcrumb: Breadcrumb;
    @Input() lastItem: boolean;
    @Output() treeClick = new EventEmitter();
    @Input() showSeperator: boolean = true;
    
    private results: SearchResult[];
    private result: SearchResult;
    public showSearch: boolean;
    private hasTree: boolean;
    public searchValue: string;
    public treeItems: TreeNode[] = [];
    public maxOverlayHeight: string = '800px'
    private searchSub: ISubscription

    constructor(private elementRef: ElementRef, private router: Router,
        private typeaheadSearchService: TypeaheadSearchService, private ref: ChangeDetectorRef) { }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.breadcrumb)
            this.treeItems = this.breadcrumb.treeItems;
    }

    ngOnInit() {
        this.setMaxHeight();
    }

    ngOnDestroy() {
        if (this.searchSub)  this.searchSub.unsubscribe(); 
    }

    private setMaxHeight() {
        this.maxOverlayHeight = (window.innerHeight > 100) ? ((window.innerHeight - 120) + 'px') : '100px';
    }

    private isChangableItem() {
        return (this.lastItem && this.breadcrumb.objectType && this.breadcrumb.objectId && !this.isTreeItem());
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
        
        this.searchSub = this.typeaheadSearchService.getObjectTypeItems(10, event.query, this.breadcrumb.objectType, this.breadcrumb.objectId).pipe(
            debounceTime(400))
            .subscribe(data => {
                this.results = data;
                this.ref.markForCheck();
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
