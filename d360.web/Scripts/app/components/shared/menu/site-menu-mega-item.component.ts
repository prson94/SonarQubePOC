﻿import { Input, Component, Output, EventEmitter, ChangeDetectionStrategy, OnInit } from "@angular/core";
import { Router } from '@angular/router';
import { BaseComponent } from '../base.component';
import { SiteMenuService } from '../../../services/site-menu.service';
import { SiteMenuItem, NavigationState } from '../../../models/site-menu.model';
import { StringConstants } from "../../../static/string-constants";

@Component({
    selector: 'd3s-site-menu-mega-item',    
    template: ` 
                <a (click)="itemClick()" [ngClass]="{'menu-item truncate':true , 'dim': item.Url == null}">
                    <div class="mega-item-container" [ngStyle]="{'text-indent': getMainIndent()}">
                        <div class="caret" (click)="handleArrowClick($event)">
                            <i *ngIf="item.Items" [class]="!item.ShowChildren ? 'subitem fa fa-caret-right' : 'subitem fa fa-caret-down'" aria-hidden="true"></i>
                        </div>
                        <div class="mega-item-title" [ngStyle]="{'text-indent': getSubIndent()}" [innerHTML]="highlight() | safeHtml"></div>
                        <div *ngIf="count > 0" class="d3s-badge pull-right">{{count}}</div>
                        <ng-container *ngIf="item.IsHomePage">&nbsp;&nbsp;<span class="fa fa-home home-icon"></span></ng-container>
                    </div>
                </a>
                <div *ngIf="item.ShowChildren">
                    <d3s-site-menu-mega-item [category] = "category" [parentUrl]="item.Url" *ngFor="let sub of item.Items" [item]="sub" [level]="level + 1" [searchText]="searchText" [active]="active" [count]="sub.count" (activeChange)="active=$event;activeChange.emit(active);"></d3s-site-menu-mega-item>                
                </div>
                `,
    changeDetection: ChangeDetectionStrategy.OnPush    
})

export class SiteMenuMegaItemComponent extends BaseComponent {
   
    @Input() item: SiteMenuItem;
    @Input() category: string;
    @Input() parentUrl: string;
    @Input() level: number;
    @Input() active: boolean;
    @Input() count: number;
    @Input() searchText: string;
    @Output() activeChange = new EventEmitter();
    numberLoading: boolean;    

    constructor(private router: Router, private menuService: SiteMenuService) {
        super();
    }

    getSubIndent() {
        if (this.level > 0 && this.item.Items == null)
            return ((this.level + 1) * 20) + 'px';
        if (this.level > 0 && this.item.Items != null)
            return '0px';
        else
            return null;
    }

    getMainIndent() {
        if (this.item.Items && this.level == 0) 
            return '0px';
        else if (this.level > 0 && this.item.Items == null)
            return ((this.level + 1) * 20) + 'px';
        else if (this.level > 0 && this.item.Items != null)
            return ((this.level) * 20) + 'px';
        else 
            return '20px';
        
    }

    handleArrowClick(event) {
        event.stopPropagation();
        
        if (!this.item.ShowChildren) {             
            this.item.ShowChildren = true;
            this.showChildElements();
        }
        else {
            this.item.ShowChildren = false;
            this.hideChildElements();
        }
    }
    public highlight() {
        if (!this.searchText) {
            return this.item.Name;
        }
        return this.item.Name.replace(new RegExp(this.searchText, "gi"), match => {
            return '<span style="background: #fd7e0e;">' + match + '</span>';
        });
    }

    itemClick() {
        if (this.item.Url == null)
            return;

        if (this.item.IsLink) {
            window.location.href = this.item.Url;
        } else {
            if (this.category === StringConstants.MenuId_Favorites) {
                this.router.navigateByUrl(this.item.Url, {state: { "invalidateKey": true }});
            } else {
                this.router.navigateByUrl(this.item.Url);
            }
        }
        this.active = false;
        this.activeChange.emit(this.active);
    }

    showChildElements() {
        let nav: NavigationState[] = JSON.parse(localStorage.getItem("NavigationMenu"));

        //check if there's already a branch for this category
        if (nav.some((x) => x.SiteMenuID == this.category)) {
            nav.forEach(menu => {
                if (menu.SiteMenuID == this.category) {
                    menu.DisplayElements.push({ ParentUrl: this.parentUrl, Url: this.item.Url ? this.item.Url : this.item.Name });
                }
            });
        } else {
            //add new category
            nav.push({ SiteMenuID: this.category, DisplayElements: [{ ParentUrl: this.parentUrl, Url: this.item.Url ? this.item.Url : this.item.Name }] });
        }

        localStorage.setItem("NavigationMenu", JSON.stringify(nav));
    }

    hideChildElements() {
        let nav: NavigationState[] = JSON.parse(localStorage.getItem("NavigationMenu"));

        nav.forEach(menu => {
            if (menu.SiteMenuID == this.category) {
                menu.DisplayElements.splice(menu.DisplayElements.findIndex(element => (element.ParentUrl == this.parentUrl && element.Url == this.item.Url) || (!element.ParentUrl && element.Url == this.item.Name)), 1)
            }
        });

        localStorage.setItem("NavigationMenu", JSON.stringify(nav));
    }
}