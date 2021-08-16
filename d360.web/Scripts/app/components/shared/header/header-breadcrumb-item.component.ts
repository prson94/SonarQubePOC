import {debounceTime} from 'rxjs/operators';
import { Component, Input, ElementRef, OnChanges, SimpleChange, Output, EventEmitter, OnInit,OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef, ViewChild } from '@angular/core';
import { Router }       from '@angular/router';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { TypeaheadSearchService } from '../../../services/typeahead-search.service';
import { SearchResult } from '../../../models/search-result.model';
import { TreeNode } from 'primeng/api';
import { SubscriptionLike as ISubscription } from 'rxjs';

@Component({
    selector: 'd3s-header-breadcrumb-item',
    providers: [TypeaheadSearchService],    
    host: {
        '(window:resize)': 'setMaxHeight()'
    },  
    template: ` <div #hovertarget class="hover-container" (mouseenter)="in(treePanel,searchPanel,$event)" (mouseleave)="out(treePanel,searchPanel,$event)" >
                    <a (click)="navigateToLink(breadcrumb.link)" 
                            class="breadcrumb" 
                            [ngClass]="{'breadcrumb-link' : hasLink(breadcrumb.link)}"
                            [ngStyle]="{'max-width.px': setLastBreadcrumbWidth()}">
                            <span class="breadcrumb-text" [ngClass]="{'highlight' : breadcrumb.isType, 'breadcrumb-link' : hasLink(breadcrumb.link)}">{{breadcrumb.text}} </span>
                            <span class="parent"  [ngClass]="{'breadcrumb-link' : hasLink(breadcrumb.link)}" *ngIf="breadcrumb.parentTypeName"   
                                  (click)="stopParentNav($event);navigateToLink(breadcrumb.parentUrl)">{{breadcrumb.parentTypeName}}</span>
                            <div *ngIf="!isChangableItem()" class="gutter"></div>
                            <i *ngIf="isChangableItem()" class="fa fa-caret-right crumb-arrow right"></i>
                    </a>
                    <div [ngClass]="'search-results'" #searchPanel>  
                        <div class="breadcrumb-search">
                            <span class="header-search-input"><input #standardInput type="text" [(ngModel)]="searchValue" placeholder="Search" (keyup)="search(searchValue)"> <span *ngIf="searchingTypeahed" class="spinner"></span><i *ngIf="!searchingTypeahed" class="fa fa-search"></i></span> 
                            <div *ngFor="let result of results;" class="breadcrumb-search-results">
                                <div class="breadcrumb-search-result" [ngClass]="{'current-crumb': breadcrumb.text === result.Name}" (click)="navigateToLink(result.Url,result)">{{result.Name}}</div>
                            </div>
                        </div>
                    </div>                
                    <div *ngIf="!isLastItem && showSeperator" class="sep breadcrumb"><i class="fa fa-angle-right"></i></div>                
                    <div [ngClass]="'search-results'" #treePanel>  
                        <div class="breadcrumb-search tree-breadcrumb-panel">    
                            <span class="header-search-input"><input #treeInput type="text" [(ngModel)]="searchTreeValue" placeholder="Search"> <i class="fa fa-search"></i></span> 
                            <p-tree [value]="treeItems | treeSearch: searchTreeValue" selectionMode="single" [(selection)]="breadcrumb.selectedTreeNode" styleClass="breadcrumbTree" [style]="{'max-height':maxOverlayHeight,'overflow':'auto','line-height':'25px'}"
                                (onNodeSelect)="nodeSelect($event,treePanel)">
                                <ng-template let-node pTemplate type="default">
                                    <span class="breadcrumb-search-result" [ngClass]="{'current-crumb': breadcrumb.text === node.label}" [ngStyle]="setTreeNodeStyles(node)">{{node.label}}  <i *ngIf="node.data?.hasRelations" class="fa fa-share-alt" aria-hidden="true" title="Item has relationships" style="color:#999;"></i></span>
                                </ng-template>
                            </p-tree>
                        </div>
                    </div>
                </div>
          `,

    changeDetection: ChangeDetectionStrategy.OnPush
})

export class HeaderBreadcrumbItemComponent implements OnChanges, OnInit, OnDestroy {    
    @Input() breadcrumb: Breadcrumb;
    @Input() isLastItem: boolean;
    @Input() lastItem: Breadcrumb;
    @Output() treeClick = new EventEmitter();
    @Input() showSeperator: boolean = true;
    @Input() index: number;
    @Input() maxLastCrumbWidth: number;
    @ViewChild('hovertarget', { static: false }) hoverTarget: ElementRef;

    @ViewChild('standardInput', { static: false }) standardInput: ElementRef;
    @ViewChild('treeInput', { static: false }) treeInput: ElementRef;

    results: SearchResult[];
    private result: SearchResult;
    public showSearch: boolean;
    private hasTree: boolean;
    public searchValue: string;
    public searchTreeValue: string;
    public treeItems: TreeNode[] = [];
    public maxOverlayHeight: string = '800px'
    private searchSub: ISubscription
    searchingTypeahed: boolean = false;
    
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

    isChangableItem() {
        return (this.breadcrumb.objectType && (+this.breadcrumb.objectId > -1)) || this.breadcrumb.treeItems;
    }

    isTreeItem(): boolean {
        return (this.breadcrumb.treeItems && this.breadcrumb.treeItems.length > 0);
    }

    in(panel, searchPanel, event) {
        let parent = this.hoverTarget.nativeElement.parentNode;
        let lineDims = this.hoverTarget.nativeElement.getBoundingClientRect();

        if (this.isChangableItem() && !this.isTreeItem()) {
            searchPanel.style.display = "block";
            this.standardInput.nativeElement.focus();
            searchPanel.style.maxWidth = (window.innerWidth - lineDims.left) + "px";
            if (this.hasClass(parent, 'collapsed-crumb')) {
                searchPanel.style.left = lineDims.right + "px";
                this.checkIsToofarRight(searchPanel);
            }
        }
        if (this.isTreeItem()) {
            panel.style.display = "block";
            panel.style.maxWidth = (window.innerWidth - lineDims.left) + "px";
            if (this.hasClass(parent, 'collapsed-crumb')) {
                panel.style.left = lineDims.right + "px";
                this.checkIsToofarRight(searchPanel); 
            }
            this.treeInput.nativeElement.focus();
        }
    }    

    out(treePanel, searchPanel, event) {
        if (this.isChangableItem()) {
            this.showSearch = true;
            searchPanel.style.display = "none";
        }
        if (this.isTreeItem()) {
            treePanel.style.display = "none";
        }
    }

    checkIsToofarRight(panel) {
        let dims = panel.getBoundingClientRect();
        if (dims.right > window.innerWidth) {
            panel.style.right = "0px";
            panel.style.left = "unset";
        }

    }

    search(event) {

        let q: string = event.query ? event.query : event;
        this.searchingTypeahed = true;
        if (this.breadcrumb.hasPreLoadedTypeAhead()) {
            this.results = this.breadcrumb.preLoadedTypeAhead.filter(x => x.Name.toLowerCase().indexOf(q.toLowerCase()) !== -1);
            this.searchingTypeahed = false;
            this.ref.markForCheck();
            return;
        }

        if (this.breadcrumb.isType) {
            if (this.breadcrumb.hasParent) {
                this.typeaheadSearchService.getObjectTypeItemsFromParent(10, q, this.breadcrumb.objectType, this.breadcrumb.objectId).pipe(
                    debounceTime(400))
                    .subscribe(data => {
                        this.results = data;
                        this.searchingTypeahed = false;
                        this.ref.markForCheck();
                    });
            } else {
                this.typeaheadSearchService.getObjectTypeItems(10, q, this.breadcrumb.objectType).pipe(
                    debounceTime(400))
                    .subscribe(data => {
                        this.results = data;
                        this.searchingTypeahed = false;
                        this.ref.markForCheck();
                    });
            }
        } 
        else {
            this.searchSub = this.typeaheadSearchService.getObjectItems(10, q, this.breadcrumb.objectType, this.breadcrumb.objectId).pipe(
                debounceTime(400))
                .subscribe(data => {
                    this.results = data;
                    this.searchingTypeahed = false;
                    this.ref.markForCheck();
                });
        }
    }

    selectItem() {
        this.router.navigateByUrl(this.result.Url);
    }
    
    nodeSelect(event, panel) {
        this.breadcrumb.text = event.node.label;
        this.treeClick.emit({ id: event.node.data.id });      
    }

    setTreeNodeStyles(node) {
        console.log(node);
        if (!node.data) return null;

        let styles = {            
            'font-weight': node.data.hasRelations ? 'bold' : 'normal',            
        };
        return styles;
    }

    setLastBreadcrumbWidth() {
        if (!this.isLastItem || !this.maxLastCrumbWidth)
            return;
        //take 80 for the collapsed menu button
        return this.maxLastCrumbWidth - 80;

    }

    stopParentNav(event) {
        event.stopPropagation();
    }

    navigateToLink(url: string, res?: any) {
        if (url && url.length > 0) 
            this.router.navigateByUrl(url);
    }

    hasLink(url: string) {
        if (url && url.length > 0 && !this.isLastItem) return true;
        else false;
    }

    hasClass(element, className) {
        return (' ' + element.className + ' ').indexOf(' ' + className + ' ') > -1;
    }
}
