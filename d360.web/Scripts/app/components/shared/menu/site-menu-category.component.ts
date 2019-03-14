import { Input, Component, OnInit, ChangeDetectionStrategy, Output, EventEmitter, AfterViewInit} from '@angular/core';
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

@Component({
    selector: 'd3s-site-menu-category',    
    template: ` 
                    <li #item [ngClass]="{'menu-category':true,'menu-parent':menu && (menu.NavigationItems),'menu-active':menu?.isActiveItem}" [pTooltip]="(!expanded && !menuhasItems(menu)) ? title : null" tooltipZIndex="10001" (mouseenter)="show(item)" (mouseleave)="hide(item)" [routerLink]="url ? url : []" style="cursor: pointer;" >
                        <span *ngIf="menuhasItems(menu)">
                            <i *ngIf="rootIconName" [class]="'fa ' + rootIconName"></i>
                            <img *ngIf="imageUrl" [src]="imageUrl" style="max-width: 15px; max-height: 15px;" />
                        </span>
                        <span *ngIf='expanded'> {{title}} <i *ngIf="menu && menu.NavigationItems && menu.NavigationItems.length > 0" class="fa fa-angle-right pull-right menu-category"></i></span>
                        <div *ngIf="menu && menu.NavigationItems && menu.NavigationItems.length > 0" class="menu-child megamenu-panel" (click)="stopNavigation($event)">
                            <div>
                                <div class="row megamenu-title truncate">
                                <span><input type="search" [(ngModel)]=searchText placeholder="{{title}}"/><i class="fa fa-search"></i></span>
                                    <span class="megamenu-tools" *ngIf="showClearButton">
                                        <i (click)="clearClick.emit(true)" class="fa fa-eraser" [pTooltip]="'Clear ' + title + ' List'" tooltipZIndex="10001"></i>
                                    </span>
                                </div>
                                <div class="row megamenu-items">
                                    <div [class]="getColumnClass(menu)" *ngFor="let item of menu.NavigationItems | simpleSearch: {Name:searchText}">
                                        <ul class="menu-group">                                        
                                            <d3s-site-menu-mega-item [item]="item" [level]="0" [(active)]="menu.isActiveItem" [countTest]="item.count"></d3s-site-menu-mega-item>
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
    private searchText: string;

    constructor(private menuService: SiteMenuService) {
        super();
    }    
    
    show(item) {        
        if (this.menu && this.menu.NavigationItems) {
            let submenu = item.children[this.expanded ? 1 : 0].nextElementSibling;

            if (submenu) {
                this.menu.isActiveItem = true;
                
                submenu.style.zIndex = ++SiteNav.zindex;

                submenu.style.top = '0px';

                submenu.style.left = item.offsetWidth + 'px';
                
                window.setTimeout(() => {                    
                    this.repositionMenuToFit(window.innerHeight, submenu);                    
                }, 150);
            }
        }
    }

    ngAfterViewInit(): void {
        this.viewReady = true;
        if (this.menuhasItems(this.menu)) {
            for (let item of this.menu.NavigationItems) {
                this.menuService.getItemCount(this.menu.Title, item.Name).then((res) => item.count = res);
            }
        }
    }

    private menuhasItems(menu) {
        return menu && menu.NavigationItems && menu.NavigationItems.length > 0;
    }

    private toggle(item) {
        this.showing = !this.showing;
        if (this.showing)
           this.show(item);
        else 
            this.hide(item);
        
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
        if(this.menu)
            this.menu.isActiveItem = false;
    }

    private getColumnClass(menu: SiteMenu) {
        let len = menu.NavigationItems.length;
        return "col s12";
        switch (len) {
            case 1: 
                return "col s12";
            case 2:
            case 3:
                return "col s6";
            case 4:
                return "col s6";
            default:
                return "col s6";
        }
    }
};