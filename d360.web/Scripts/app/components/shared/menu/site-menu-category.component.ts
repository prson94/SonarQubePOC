import { Input, Component, OnInit, ChangeDetectionStrategy} from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../base.component';
import { SiteMenuService, AuthenticationService, StateService, FavoritesService } from '../../../services/index';
import { SiteMenu, SiteMenuItem, SiteNav } from '../../../models/site-menu.model';
import { Favorite } from '../../../models/favorite.model';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';

@Component({
    selector: 'd3s-site-menu-category',    
    template: ` 
                    <li #item [ngClass]="{'menu-category':true,'menu-parent':menu && (menu.NavigationItems),'menu-active':menu?.isActiveItem}" (mouseenter)="show(item)" (mouseleave)="hide(item)">
                        <span *ngIf="menu && menu.NavigationItems"><i [class]="'fa ' + rootIconName" [routerLink]="url"></i></span>
                        <span *ngIf="!menu || !menu.NavigationItems" [pTooltip]="rootToolTip"><i [class]="'fa ' + rootIconName" [routerLink]="url"></i></span>
                        <div *ngIf="menu && menu.NavigationItems" class="menu-child megamenu-panel">
                            <div>
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
    @Input() rootToolTip: string;
    @Input() rootIconName: string;
    @Input() menu: SiteMenu;

    constructor() {
        super();
    }    
    
    show(item) {        
        if (this.menu && this.menu.NavigationItems) {
            let submenu = item.children[0].nextElementSibling;            
            if (submenu) {
                this.menu.isActiveItem = true;

                submenu.style.zIndex = ++SiteNav.zindex;

                submenu.style.top = '0px';
                submenu.style.left = item.offsetWidth + 'px';
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