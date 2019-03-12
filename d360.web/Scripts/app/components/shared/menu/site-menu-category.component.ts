import { Input, Component, OnInit, ChangeDetectionStrategy, Output, EventEmitter} from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../base.component';
import { StateService } from '../../../services/state.service';
import { FavoritesService } from '../../../services/favorites.service';
import { AuthenticationService } from '../../../services/authentication.service';
import { SiteMenuService } from '../../../services/site-menu.service';
import { SiteMenu, SiteMenuItem, SiteNav } from '../../../models/site-menu.model';
import { Favorite } from '../../../models/favorite.model';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';

@Component({
    selector: 'd3s-site-menu-category',    
    template: ` 
                    <li #item [ngClass]="{'menu-category':true,'menu-parent':menu && (menu.NavigationItems),'menu-active':menu?.isActiveItem}" (click)="toggle(item)">
                        <span *ngIf="menu && menu.NavigationItems && menu.NavigationItems.length > 0" [pTooltip]="title" tooltipZIndex="10001"><i *ngIf="url" [class]="'fa ' + rootIconName" [routerLink]="url"></i><i *ngIf="!url" [class]="'fa ' + rootIconName"></i></span>
                        <span *ngIf="!menu || !menu.NavigationItems || menu.NavigationItems.length == 0" [pTooltip]="title" tooltipZIndex="10001"><i [class]="'fa ' + rootIconName" [routerLink]="url"></i></span>
                        <span *ngIf='expanded'> {{title}} <i *ngIf="menu && menu.NavigationItems && menu.NavigationItems.length > 0" class="fa fa-angle-right pull-right menu-category"></i></span>
                        <div *ngIf="menu && menu.NavigationItems && menu.NavigationItems.length > 0" class="menu-child megamenu-panel">
                            <div>
                                <div class="megamenu-title truncate">
                                    <span *ngIf="expanded"><input style="color: #000000;" type="search" placeholder="{{title}}"/> </span>
                                    <span *ngIf="!expanded">{{title}} </span>
                                    <span class="megamenu-tools" *ngIf="showClearButton">
                                        <i (click)="clearClick.emit(true)" class="fa fa-eraser" [pTooltip]="'Clear ' + title + ' List'" tooltipZIndex="10001"></i>
                                    </span>
                                </div>
                                <div class="row">
                                    <div [class]="getColumnClass(menu)" *ngFor="let item of menu.NavigationItems">
                                        <ul class="menu-group">                                        
                                            <d3s-site-menu-mega-item [item]="item" [level]="0" [(active)]="menu.isActiveItem"></d3s-site-menu-mega-item>
                                        </ul>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </li>                    
                `,   
    changeDetection: ChangeDetectionStrategy.OnPush    
})

export class SiteMenuCategoryComponent extends BaseComponent {
    @Input() url: string;
    @Input() title: string;
    @Input() rootIconName: string; 
    @Input() menu: SiteMenu;
    @Input() showClearButton: boolean = false;
    @Input() expanded: boolean;

    @Output() clearClick = new EventEmitter();
    
    public showing: boolean = false;

    constructor() {
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

    private toggle(item) {
        this.showing = !this.showing;
        if (this.showing)
           this.show(item);
        else 
            this.hide(item);
        
    }

    repositionMenuToFit(windowHeight, element) {        
        var dims = element.getBoundingClientRect();

        if (dims) {
            var maxHeight = dims.top + dims.height;

            //case where menu is bigger than height of page
            if (dims.height > windowHeight) {                
                element.style.height = windowHeight + 'px';
                element.style.overflow = 'auto';
                element.style.top = '-'+ element.style.top + 'px';
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