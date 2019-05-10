import { Input, Component, OnInit, ChangeDetectionStrategy, Output, EventEmitter, AfterViewInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../base.component';
import { StateService } from '../../../services/state.service';
import { FavoritesService } from '../../../services/favorites.service';
import { AuthenticationService } from '../../../services/authentication.service';
import { SiteMenuService } from '../../../services/site-menu.service';
import { SiteMenu, SiteMenuItem, SiteNav } from '../../../models/site-menu.model';
import { Favorite } from '../../../models/favorite.model';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { forEach } from '@angular/router/src/utils/collection';
import { HeaderActionsService } from '../../../services/header-actions.service';
import { isString, isArray } from 'util';
import { stringify } from '@angular/core/src/util';
import { createWriteStream } from 'fs';

@Component({
    selector: 'd3s-site-menu-category',
    template: ` 
                    <li #item [ngClass]="{'menu-category':true,'menu-parent':menu && (menu.NavigationItems),'menu-active':menu?.isActiveItem}" title="{{title}}" (mouseenter)="show(item); clearSearches(event, item);" (mouseleave)="hide(item);" [routerLink]="url ? url : []" style="cursor: pointer;" >
                       <div class="menu-category-box">
                            <i *ngIf="rootIconName" [class]="'fa ' + rootIconName"></i>
                            <img *ngIf="imageUrl" [src]="imageUrl" />
                            <div [ngClass]="{'caption':true, 'min':!expanded}">
                                <div [ngClass]="{'no-overflow':expanded, 'icon-active':expanded, 'icon':!expanded}"> {{title}} </div>
                                <i [ngClass]="{'pull-right menu-category fa fa-caret-right':(menu && menu.NavigationItems && menu.NavigationItems.length > 0),'icon-active':expanded, 'icon':!expanded}"></i>
                            </div>
                        </div>
                        <div #panel *ngIf="menu && menu.NavigationItems && menu.NavigationItems.length > 0" class="menu-child megamenu-panel" title="" [ngStyle]="{'display:flex; flex-direction:column': menu.isActiveItem}" (click)="stopNavigation($event)" (keyup)="checkKey($event,panel)">
                            <div class="ie-min-content">
                                <div class="row megamenu-title truncate">
                                    <input (keyup)="positionMenu($event,item)" #searchinput type="search" [(ngModel)]=searchText placeholder="Search menu..."/>
                                    <i (click)="clearInput()" [ngClass]="{'fa fa-times':searchText != '', 'fa fa-search':searchText == '' ||  !seachtext}"></i>
                                </div>
                                    <span class="megamenu-tools" *ngIf="showClearButton">
                                        <i (click)="clearClick.emit(true)" class=" pull-right fa fa-eraser" [pTooltip]="'Clear ' + title + ' List'" tooltipZIndex="10001"></i>
                                    </span>
                                <div class="row megamenu-items"[ngStyle]="{'max-height': getMaxHeight()}">
                                    <div class="col s12 megamenu-items-container" *ngFor="let item of menu.NavigationItems | simpleSearch: searchText">
                                        <ul class="menu-group">                                        
                                            <d3s-site-menu-mega-item [item]="item" [level]="0" [searchText]="searchText" [(active)]="menu.isActiveItem" [count]="item.count"></d3s-site-menu-mega-item>
                                        </ul>
                                    </div> 
                                </div>
                            </div>
                        </div>
                    </li>                    
                `,
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

    @Output() clearClick = new EventEmitter();
    @Output() clearSearchesEvent = new EventEmitter();

    public showing: boolean = false;
    private viewReady: boolean;
    private maxMenuHeight: number; 
    public searchText: string = "";

    private subReloadCounts: any;
    private currentButtonIndex: number = -1;

    constructor(private menuService: SiteMenuService,
        private headerActionsService: HeaderActionsService) {
        super();
    }

    @ViewChild('searchinput') searchInput: any;


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
            allAItems[this.currentButtonIndex].style['background-color'] = "#878b97";
        }
    }
  
    ResetColor(allAItems) {
        if (allAItems.length) {
            Array.prototype.forEach.call(allAItems, function (item) {
                item.style['background-color'] = "#4e5466";
            });
        }
    }
    show(item) {
        if (this.menu && this.menu.isActiveItem)
            return;
        this.positionMenu(null,item);
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
                    this.searchInput.nativeElement.focus();
                }, 350);
              
                window.setTimeout(() => {
                    this.repositionMenuToFit(submenu);
                }, 150);
            }
        }
    }

    loadCounts() {
        if (this.menu && this.menu.NavigationItems && this.menu.NavigationItems.length > 0 && !this.menu.MenuID.startsWith('-')) {
            this.menu.NavigationItems.forEach((item) => this.getCount(item));
        }
    }

    getCount(items) {
        if (isString(items.Name) && isString(items.Url) && items.Url.indexOf('/') != -1) {
            //get count for item
            this.menuService.getItemCount(items.Url.replace(new RegExp('/', 'g'), '-')).then((res) => { items.count = res });
        }

        //check if sub items exist
        if (isArray(items.Items)) {
            //recursively check sub items
            items.Items.forEach((item) => this.getCount(item));
        }
    }

    ngAfterViewInit(): void {

        this.subReloadCounts = this.headerActionsService.onSiteCountsChange.subscribe(() => {
            this.loadCounts();
        });

        this.viewReady = true;

        if (this.searchInput) {
            this.searchInput.nativeElement.focus();
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