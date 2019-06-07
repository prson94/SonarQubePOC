
import {debounceTime, debounce} from 'rxjs/operators';
import { Component, Input, ElementRef, ViewChildren, OnChanges, SimpleChange, Output, EventEmitter, AfterViewInit, OnInit,OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef, ViewChild } from '@angular/core';
import { Router }       from '@angular/router';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { TypeaheadSearchService } from '../../../services/typeahead-search.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SearchResult } from '../../../models/search-result.model';
import { TreeNode } from 'primeng/components/common/api';
import { SubscriptionLike as ISubscription } from 'rxjs';
import { createWriteStream } from 'fs';
import { clearLine } from 'readline';

@Component({
    selector: 'd3s-header-breadcrumb-item',
    providers: [TypeaheadSearchService],    
    host: {
       // '(document:click)': 'onClick($event)',
        '(window:resize)': 'setMaxHeight()'
    },  
    template: ` <div #hovertarget class="hover-container" (mouseenter)="in(treePanel,searchPanel,$event)" (mouseleave)="out(treePanel,searchPanel,$event)" >
                    <a id="breadlink" (click)="navigateToLink(breadcrumb.link)" 
                            class="breadcrumb" 
                            style="cursor:pointer">
                            <span class="breadcrumb-text">{{breadcrumb.text}} </span>
                            <span class="parent" *ngIf="breadcrumb.parentTypeName"   
                                  (click)="stopParentNav($event);navigateToLink(breadcrumb.parentUrl)">
                                    {{breadcrumb.parentTypeName}}
                            </span>
                            <div *ngIf="!isChangableItem()" class="gutter"></div>
                            <i *ngIf="isChangableItem()" class="fa fa-caret-right crumb-arrow right"></i>
                    </a>
                    <p-overlayPanel [ngClass]="'search-results'" #searchPanel for="hovertarget" my="left top" at="top right">  
                        <div>
                            <span class="header-search-input"><input type="text" [(ngModel)]="searchValue" placeholder="Search" (keyup)="search(searchValue)"> <i class="fa fa-search"></i></span> 
                            <div *ngFor="let result of results;" class="breadcrumb-search-results">
                                <div class="breadcrumb-search-result" [ngClass]="{'current-crumb': breadcrumb.text === result.Name}" (click)="navigateToLink(result.Url)">{{result.Name}}</div>
                            </div>
                        </div>
                    </p-overlayPanel>                
                    <div *ngIf="!isLastItem && showSeperator" class="sep breadcrumb"><i class="fa fa-angle-right"></i></div>                
                    <p-overlayPanel #treePanel>  
                        <div class="tree-breadcrumb">    
                            <input type="text" pInputText [(ngModel)]="searchTreeValue" placeholder="Search" style="width: 100%;">                      
                            <p-tree [value]="treeItems | treeSearch: searchTreeValue" selectionMode="single" [(selection)]="breadcrumb.selectedTreeNode" styleClass="breadcrumbTree" [style]="{'max-height':maxOverlayHeight,'overflow':'auto','line-height':'25px'}" 
                                (onNodeSelect)="nodeSelect($event,treePanel)">
                                <ng-template let-node pTemplate type="default">
                                    <span [ngStyle]="setTreeNodeStyles(node)">{{node.label}} <i *ngIf="node.data?.hasRelations" class="fa fa-share-alt" aria-hidden="true" title="Item has relationships" style="color:#999;"></i></span>
                                </ng-template>
                            </p-tree>
                        </div>
                    </p-overlayPanel>
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
    @ViewChild('hovertarget') hoverTarget: ElementRef;
    private results: SearchResult[];
    private result: SearchResult;
    public showSearch: boolean;
    private hasTree: boolean;
    public searchValue: string;
    public searchTreeValue: string;
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
        return (this.breadcrumb.objectType && this.breadcrumb.objectId && !this.isTreeItem());
    }

    private isTreeItem(): boolean {
        return this.breadcrumb.objectType == 'Taxonomy' || this.breadcrumb.objectType == 'Policy';
    }
    
    private in(panel, searchPanel, event) {

        let parent = this.hoverTarget.nativeElement.parentNode;
        if (this.isChangableItem()) {
            this.showSearch = true;
            if (this.hasClass(parent, 'collapsed-crumb')) {
                searchPanel.show(event, this.hoverTarget.nativeElement.parentNode);
                searchPanel.el.nativeElement.children[0].opacity = 0;
                window.setTimeout(() => {
                    let lineDims = this.hoverTarget.nativeElement.getBoundingClientRect();
                    searchPanel.el.nativeElement.children[0].style.top = (lineDims.top - 30) + "px";
                    searchPanel.el.nativeElement.children[0].style.left = (lineDims.width) + "px";
                }, 100);
                
            } else {
                searchPanel.show(event);
            }
        }        
        if (this.isTreeItem()) {
            if (this.hasClass(parent, 'collapsed-crumb'))
                panel.show(event, this.hoverTarget.nativeElement.parentNode);
            else
                panel.show(event);
        }
    }    

    out(treePanel, searchPanel, event) {
        if (this.isChangableItem()) {
            this.showSearch = true;
            searchPanel.hide();
        }
        if (this.isTreeItem()) {
            treePanel.hide();
        }
    }

    search(event) {

        let q = event.query ? event.query : event;

        if (this.breadcrumb.isType) {
            if (this.breadcrumb.hasParent) {
                this.typeaheadSearchService.getObjectTypeItemsFromParent(10, q, this.breadcrumb.objectType, this.breadcrumb.objectId).pipe(
                    debounceTime(400))
                    .subscribe(data => {
                        this.results = data;
                        this.ref.markForCheck();
                    });
            } else {
                this.typeaheadSearchService.getObjectTypeItems(10, q, this.breadcrumb.objectType).pipe(
                    debounceTime(400))
                    .subscribe(data => {
                        this.results = data;
                        this.ref.markForCheck();
                    });
            }
        } else {
            this.searchSub = this.typeaheadSearchService.getObjectItems(10, q, this.breadcrumb.objectType, this.breadcrumb.objectId).pipe(
                debounceTime(400))
                .subscribe(data => {
                    this.results = data;
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
        panel.hide();
    }

    setTreeNodeStyles(node) {
        if (!node.data) return null;

        let styles = {            
            'font-weight': node.data.hasRelations ? 'bold' : 'normal',            
        };
        return styles;
    }
    private stopParentNav(event) {
        event.stopPropagation();
    }
    private navigateToLink(url: string) {

        console.log(url);
        if (url && url.length > 0)
            this.router.navigateByUrl(url);

    }
    private hasClass(element, className) {
        return (' ' + element.className + ' ').indexOf(' ' + className + ' ') > -1;
    }
}
