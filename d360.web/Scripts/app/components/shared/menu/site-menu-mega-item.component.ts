import { Input, Component, Output, EventEmitter, ChangeDetectionStrategy, OnInit} from '@angular/core';
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
    selector: 'd3s-site-menu-mega-item',    
    template: ` 
                <a (click)="itemClick()" [ngClass]="{'menu-item truncate':true , 'dim': item.Url == null}">
                    <div style="display: inline-flex;width: inherit;" [ngStyle]="{'text-indent': getMainIndent()}">
                        <div style="margin-left: -5px;padding-right: 5px;" (click)="handleArrowClick($event)">
                            <i *ngIf="item.Items" [class]="!displayChild ? 'subitem fa fa-caret-right' : 'subitem fa fa-caret-down'" aria-hidden="true"></i>
                        </div>
                        <div style="padding-right: 40px; line-height: 33px;height: 22px;" [ngStyle]="{'text-indent': getSubIndent()}" [innerHTML]="highlight() | safeHtml"></div>
                        <div *ngIf="count > 0" style="margin-left: auto;" class="d3s-badge pull-right">{{count}}</div>
                        <ng-container *ngIf="item.IsHomePage">&nbsp;&nbsp;<span style="line-height: 25px;" class="fa fa-home"></span></ng-container>
                    </div>
                </a>
                <div *ngIf="displayChild">
                    <d3s-site-menu-mega-item  *ngFor="let sub of item.Items" [item]="sub" [level]="level + 1" [searchText]="searchText" [active]="active" [count]="sub.count" (activeChange)="active=$event;activeChange.emit(active);"></d3s-site-menu-mega-item>                
                </div>
                `,
    changeDetection: ChangeDetectionStrategy.OnPush    
})

export class SiteMenuMegaItemComponent extends BaseComponent {
   
    @Input() item: SiteMenuItem;    
    @Input() level: number;
    @Input() active: boolean;
    @Input() count: number;
    @Input() searchText: string;
    @Output() activeChange = new EventEmitter();
    numberLoading: boolean;
    displayChild: boolean = true;

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

    private handleArrowClick(event) {
        event.stopPropagation();
        this.displayChild = !this.displayChild;
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

        if (this.item.IsLink)
            window.location.href = this.item.Url;
        else
            this.router.navigateByUrl(this.item.Url);

        this.active = false;
        this.activeChange.emit(this.active);
    }    
};