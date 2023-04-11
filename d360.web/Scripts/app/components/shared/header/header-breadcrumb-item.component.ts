import { debounceTime } from 'rxjs/operators';
import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    ElementRef,
    EventEmitter,
    Input,
    OnChanges,
    OnDestroy,
    OnInit,
    Output,
    SimpleChange,
    ViewChild
} from '@angular/core';
import { Router } from '@angular/router';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { TypeaheadSearchService } from '../../../services/typeahead-search.service';
import { SearchResult } from '../../../models/search-result.model';
import { TreeNode } from 'primeng/api';
import { SubscriptionLike as ISubscription } from 'rxjs';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';

@Component({
    selector: 'd3s-header-breadcrumb-item',
    providers: [TypeaheadSearchService],    
    host: {
        '(window:resize)': 'setMaxHeight()'
    },  
    templateUrl: './header-breadcrumb-item.component.html',
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
    public maxOverlayHeight: string = '800px';
    private searchSub: ISubscription;
    searchingTypeahed: boolean = false;
    
    constructor(private elementRef: ElementRef,
				private router: Router, 
				private typeaheadSearchService: TypeaheadSearchService,
				private ref: ChangeDetectorRef) { }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.breadcrumb)
            {this.treeItems = this.breadcrumb.treeItems;}
    }

    ngOnInit() {
        this.setMaxHeight();
    }

    ngOnDestroy() {
        if (this.searchSub)  {this.searchSub.unsubscribe();}
    }

    setMaxHeight() {
        this.maxOverlayHeight = (window.innerHeight > 100) ? ((window.innerHeight - 120) + 'px') : '100px';
    }

    isChangableItem() {
        return (this.breadcrumb.objectType && (+this.breadcrumb.objectId > -1)) || this.breadcrumb.treeItems;
    }

    isTreeItem(): boolean {
        return (this.breadcrumb.treeItems && this.breadcrumb.treeItems.length > 0);
    }

    in(panel, searchPanel, event) {
        const parent = this.hoverTarget.nativeElement.parentNode;
        const lineDims = this.hoverTarget.nativeElement.getBoundingClientRect();

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
        const dims = panel.getBoundingClientRect();
        if (dims.right > window.innerWidth) {
            panel.style.right = "0px";
            panel.style.left = "unset";
        }

    }

    search(event) {

        const q: string = event.query ? event.query : event;
        this.searchingTypeahed = true;
        if (this.breadcrumb.hasPreLoadedTypeAhead()) {
            this.results = this.breadcrumb.preLoadedTypeAhead.filter((x) => x.Name.toLowerCase().indexOf(q.toLowerCase()) !== -1);
            this.searchingTypeahed = false;
            this.ref.markForCheck();
            return;
        }

        if (this.breadcrumb.isType) {
            if (this.breadcrumb.hasParent) {
                this.typeaheadSearchService.getObjectTypeItemsFromParent(10, q, this.breadcrumb.objectType, this.breadcrumb.objectId).pipe(
                    debounceTime(400))
                    .subscribe((data) => {
                        this.results = data;
                        this.searchingTypeahed = false;
                        this.ref.markForCheck();
                    });
            } else {
                this.typeaheadSearchService.getObjectTypeItems(10, q, this.breadcrumb.objectType).pipe(
                    debounceTime(400))
                    .subscribe((data) => {
                        this.results = data;
                        this.searchingTypeahed = false;
                        this.ref.markForCheck();
                    });
            }
        } 
        else {
            this.searchSub = this.typeaheadSearchService.getObjectItems(10, q, this.breadcrumb.objectType, this.breadcrumb.objectId).pipe(
                debounceTime(400))
                .subscribe((data) => {
                    this.results = data;
                    this.searchingTypeahed = false;
                    this.ref.markForCheck();
                });
        }
    }

    selectItem() {
		this.router.navigateByUrl(SiteUrlHelpers.federateUrl(this.result.Url));
    }
    
    nodeSelect(event, panel) {
        this.breadcrumb.text = event.node.label;
        this.treeClick.emit({ id: event.node.data.id });      
    }

    setTreeNodeStyles(node) {
        console.log(node);
        if (!node.data) {return null;}

        const styles = {            
            'font-weight': node.data.hasRelations ? 'bold' : 'normal',            
        };
        return styles;
    }

    setLastBreadcrumbWidth() {
        if (!this.isLastItem || !this.maxLastCrumbWidth)
            {return;}
        //take 80 for the collapsed menu button
        return this.maxLastCrumbWidth - 80;

    }

    stopParentNav(event) {
        event.stopPropagation();
    }

    navigateToLink(url: string, res?: any) {
		if (url && url.length > 0) {
			this.router.navigateByUrl(SiteUrlHelpers.federateUrl(url));
		}
    }

    hasLink(url: string) {
        return url && url.length > 0 && !this.isLastItem;
    }

    hasClass(element, className) {
        return (' ' + element.className + ' ').indexOf(' ' + className + ' ') > -1;
    }
}