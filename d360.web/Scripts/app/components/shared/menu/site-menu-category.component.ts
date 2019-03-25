import { Input, Component, OnInit, ChangeDetectionStrategy, Output, EventEmitter, AfterViewInit, ViewChild} from '@angular/core';
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
                    <li #item [ngClass]="{'menu-category':true,'menu-parent':menu && (menu.NavigationItems),'menu-active':menu?.isActiveItem}" title="{{title}}" (mouseenter)="show(item)" (mouseleave)="hide(item)" [routerLink]="url ? url : []" style="cursor: pointer;" >
                        <span>
                            <i *ngIf="rootIconName" [class]="'fa ' + rootIconName"></i>
                            <img *ngIf="imageUrl" [src]="imageUrl" style="max-width: 15px; max-height: 15px; margin: 0px 15px 0px 12px" />
                            <span [ngClass]="{'caption':true, 'min':!expanded}">
                                <span [ngClass]="{'icon-active':expanded, 'icon':!expanded}"> {{title}} <i [ngClass]="{'pull-right menu-category fa fa-caret-right':(menu && menu.NavigationItems && menu.NavigationItems.length > 0)}"></i></span>
                            </span>
                        </span>
                        <div *ngIf="menu && menu.NavigationItems && menu.NavigationItems.length > 0" class="menu-child megamenu-panel" (click)="stopNavigation($event)">
                            <div>
                                <div class="row megamenu-title truncate">
                                    <span>
                                        <input #searchinput type="search" [(ngModel)]=searchText placeholder="Search menu..."/>
                                        <i *ngIf="searchText != ''" (click)="clearInput()" class="fa fa-times"></i>
                                    </span>
                                </div>
                                    <span class="megamenu-tools" *ngIf="showClearButton">
                                        <i (click)="clearClick.emit(true)" class=" pull-right fa fa-eraser" [pTooltip]="'Clear ' + title + ' List'" tooltipZIndex="10001"></i>
                                    </span>
                                <div class="row megamenu-items">
                                    <div  style="padding:0px;" [class]="getColumnClass(menu)" *ngFor="let item of menu.NavigationItems | simpleSearch: searchText">
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


    public showing: boolean = false;
    private viewReady: boolean;
    private maxMenuHeight: number; 
    private searchText: string = '';
    private subReloadCounts: any;

    constructor(private menuService: SiteMenuService,
        private headerActionsService: HeaderActionsService) {
        super();
    }    

    @ViewChild('searchinput') searchInput: any;
    
    show(item) {        
        if (this.menu && this.menu.NavigationItems) {
            let submenu = item.children[0].nextElementSibling;

            if (submenu) {
                this.menu.isActiveItem = true;
                
                submenu.style.zIndex = ++SiteNav.zindex;

                submenu.style.top = '0px'; 

                submenu.style.left = item.offsetWidth + 'px';

                window.setTimeout(() => {
                    this.searchInput.nativeElement.focus();
                }, 350);

                window.setTimeout(() => {                    
                    this.repositionMenuToFit(window.innerHeight, submenu);                    
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

    repositionMenuToFit(windowHeight, element) {        
        var dims = element.getBoundingClientRect();

        if (dims) {
            var maxHeight = dims.top + dims.height;

            //case where menu is bigger than height of page
            if (dims.height > windowHeight) {                
                element.style.height = windowHeight + 'px';
                element.style.top = '-'+ element.style.top + 'px';
                element.children[0].children[1].style.height = (windowHeight - 45) + 'px';

                //get the dim values again and check is it outside the bottom after opening and moving top
                dims = element.getBoundingClientRect();
                maxHeight = dims.top + dims.height;
                if (maxHeight > windowHeight) { //case where bottom is below page after resizing
                    var topOffset = windowHeight - maxHeight;
                    element.style.top = topOffset + 'px';
                }            
            }            
            else if (maxHeight > windowHeight) { //case where bottom is below page
                var topOffset = windowHeight - maxHeight;
                element.style.top = topOffset + 'px';
            }            
        }        
    }

    hide(item) {
        if (this.menu)
            this.menu.isActiveItem = false;
    }

    clearInput() {
        this.searchText = '';
    }

    private getColumnClass(menu: SiteMenu) {
        return "col s12";
    }
};