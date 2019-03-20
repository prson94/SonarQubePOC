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
                <a (click)="itemClick()" class="menu-item truncate" [ngStyle]="{'margin-left': getMargin()}">
                    <span (click)="handleArrowClick($event)">
                        <i *ngIf="item.Items" [class]="!displayChild ? 'subitem fa fa-caret-right' : 'subitem fa fa-caret-down'" aria-hidden="true"></i>
                    </span>
                    {{item.Name}}<ng-container *ngIf="item.IsHomePage">&nbsp;&nbsp;<span class="fa fa-home"></span></ng-container>
                    <span *ngIf="countTest > 0" class="d3s-badge pull-right">{{countTest}}</span>
                </a>
                <div *ngIf="displayChild">
                    <d3s-site-menu-mega-item  *ngFor="let sub of item.Items" [item]="sub" [level]="level + 1" [active]="active" (activeChange)="active=$event;activeChange.emit(active);"></d3s-site-menu-mega-item>                
                </div>
                `,
    changeDetection: ChangeDetectionStrategy.OnPush    
})

export class SiteMenuMegaItemComponent extends BaseComponent {
   
    @Input() item: SiteMenuItem;    
    @Input() level: number;
    @Input() active: boolean;
    @Input() countTest: number;
    @Output() activeChange = new EventEmitter();
    count: number;
    numberLoading: boolean;
    displayChild: boolean = true;

    constructor(private router: Router, private menuService: SiteMenuService) {
        super();
    }

    

    getMargin() {        
        return (this.level * 10) + 'px';
    }

    private handleArrowClick(event) {
        event.stopPropagation();
        this.displayChild = !this.displayChild;
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