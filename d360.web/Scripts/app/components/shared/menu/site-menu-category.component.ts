import { Input, Component, OnInit, ChangeDetectionStrategy, Output, EventEmitter, AfterViewInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../base.component';
import { SiteMenuService } from '../../../services/site-menu.service';
import { SiteMenu, SiteMenuItem, SiteNav } from '../../../models/site-menu.model';
import { HeaderActionsService } from '../../../services/header-actions.service';
import { isString, isArray } from 'util';
import * as _ from 'lodash';
import { SearchFieldComponent } from '../controls/search-field/search-field.component';
import { forEach } from 'lodash';

@Component({
    selector: 'd3s-site-menu-category',
    templateUrl: 'site-menu-category.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class SiteMenuCategoryComponent extends BaseComponent implements AfterViewInit {

    @Input() url: string;
    @Input() title: string;
    @Input() rootIconName: string;
    @Input() menu: SiteMenu;
    @Input() showClearButton: boolean = false;
    @Input() expanded: boolean;
    @Input() imageUrl: string;
    @Input() countData: any[];

    @Output() clearClick = new EventEmitter();
    @Output() clearSearchesEvent = new EventEmitter();

    public showing: boolean = false;
    private viewReady: boolean;
    private maxMenuHeight: number;
    public searchText: string = "";

    private currentButtonIndex: number = -1;

    constructor(private menuService: SiteMenuService,
        private router: Router,
        private headerActionsService: HeaderActionsService,
        private siteMenuService: SiteMenuService) {
        super();
    }

    @ViewChild('searchinput', { static: false }) searchInput: SearchFieldComponent;

    toggleEmptyVisibility() {
        var newValue = (!this.hideEmptyItems).toString();
        localStorage.setItem(this.storageKey, newValue)
    }

    hideEmptySubItems(items: SiteMenuItem[]) {
        items.forEach((x) => {
            if (x.Items) {
                x.Items = x.Items.filter(x => x.count > 0);
                this.hideEmptySubItems(x.Items);
            }
        });
    }

    get visibleMenuItems(): SiteMenuItem[] {
        if (this.hideEmptyItems) {
            var items = this.menu.NavigationItems.filter((x) => x.count > 0);
            this.hideEmptySubItems(items);
            return items;
        }
        else return this.menu.NavigationItems;
    }

    get showVisiblityToggle(): boolean {
        return this.menu.ShowVisibilityToggle;
    }

    get hideEmptyItems(): boolean {
        return localStorage.getItem(this.storageKey) === "true";
    }

    get storageKey(): string {
        return "hide-empty-items-" + this.menu.MenuID;
    }

    getMaxHeight() {
        return (window.innerHeight - 80) + 'px';
    }

    checkKey(event, elem) {
        if (event.keyCode == 40 || event.keyCode == 13 || event.keyCode == 38) {

            let allAItems = elem.getElementsByTagName("a");
            if (!allAItems.length)
                return;

            if (event.keyCode == 13)
                allAItems[this.currentButtonIndex].click();
            if (event.keyCode == 40) {
                this.currentButtonIndex++;
            } else if (event.keyCode == 38) {
                this.currentButtonIndex--;
            }

            if (allAItems.length - 1 < this.currentButtonIndex || this.currentButtonIndex < 0)
                this.currentButtonIndex = 0;

            this.ResetColor(allAItems);
            let arr = allAItems[this.currentButtonIndex].className.split(" ");
            if (arr.indexOf("highlight") == -1) {
                allAItems[this.currentButtonIndex].className += " highlight";
            }

        }
    }
    navigateToUrl(url) {
        if (url) {
            this.router.navigateByUrl(url);
        }
    }
    ResetColor(allAItems) {
        if (allAItems.length) {
            Array.prototype.forEach.call(allAItems, function (item) {
                item.className = item.className.replace(/\b highlight\b/g, "");
            });
        }
    }
    show(item) {
        if (this.menu && this.menu.isActiveItem)
            return;
        this.positionMenu(null, item);
    }

    private positionMenu(event: any, item: any) {
        if (event != null && (event.keyCode == 40 || event.keyCode == 13 || event.keyCode == 38)) {
            return;
        }
        if (this.menu && this.menu.NavigationItems) {
            let submenu = item.children[0].nextElementSibling;
            if (submenu) {
                var dims = item.getBoundingClientRect();
                this.menu.isActiveItem = true;
                submenu.style.zIndex = ++SiteNav.zindex;
                submenu.style.top = dims.top + 'px';
                submenu.style.left = item.offsetWidth + 'px';
                window.setTimeout(() => {
                    this.searchInput.focus();
                }, 350);

                window.setTimeout(() => {
                    this.repositionMenuToFit(submenu);
                }, 150);
            }
        }
    }

    loadCounts(menu: any) {
        if (menu && menu.NavigationItems && menu.NavigationItems.length > 0 && !menu.MenuID.startsWith('-')) {
            this.siteMenuService.getCounts().subscribe((res) => {
                menu.NavigationItems.forEach((item) => this.getAllCounts(item, res));
            });
        }
    }

    getAllCounts(items, arr: any[]) {
        if (isString(items.Name) && isString(items.Url) && items.Url.indexOf('/') != -1) {
            //get count for item
            var id = _.findIndex(arr, function (o) {
                let currentURL = items.Url.toLowerCase();
                currentURL = items.Url.replace('model', 'taxonomy');
                return o.Name == items.Name
                    && _.includes(currentURL, o.Object.toLowerCase().replace('type', ''))
                    && _.includes(currentURL, o.ObjectID);
            });
            if (id !== -1) {
                items.count = arr[id].count;
            } else {
                items.count = 0;
            }
        }

        //check if sub items exist
        if (isArray(items.Items)) {
            //recursively check sub items
            items.Items.forEach((item) => this.getAllCounts(item, arr));
        }
    }

    ngAfterViewInit(): void {

        this.viewReady = true;

        if (this.searchInput) {
            this.searchInput.focus();
        }

    }

    private menuhasItems(menu) {
        return menu && menu.NavigationItems && menu.NavigationItems.length > 0;
    }

    private stopNavigation(event) {
        event.stopPropagation();
    }

    repositionMenuToFit(element) {
        var dims = element.getBoundingClientRect();
        let windowHeight = window.innerHeight;
        if (dims) {
            var maxHeight = dims.top + dims.height;

            //case where menu is bigger than height of page
            if (dims.height > windowHeight) {
                dims = element.getBoundingClientRect();
                element.style.top = 40 + 'px';
                maxHeight = dims.top + dims.height;
                if (maxHeight > windowHeight) { //case where bottom is below page after resizing
                    var topOffset = dims.top + (windowHeight - maxHeight);
                    element.style.top = topOffset + 'px';
                }
            }
            else if (maxHeight > windowHeight) { //case where bottom is below page
                var topOffset = dims.top + (windowHeight - maxHeight);

                element.style.top = topOffset + 'px';
            }
        }
    }

    hide(item) {
        if (this.menu && this.searchText == "") {
            this.ResetColor(item.getElementsByTagName("a"));
            this.currentButtonIndex = -1;
            this.menu.isActiveItem = false;
        }
    }

    clearSearches(event, item) {
        this.clearSearchesEvent.emit({ event: event, item: item });
    }
    clearInput() {
        this.searchText = "";
    }


}