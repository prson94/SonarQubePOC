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
                   <i [class]="'fa fa-circle menu-level-indicator-' + level" aria-hidden="true"></i>{{item.Name}}<ng-container *ngIf="item.IsHomePage">&nbsp;&nbsp;<span class="fa fa-home"></span></ng-container><span *ngIf="count > 0" class="d3s-badge pull-right">{{count}}</span></a>
                <d3s-site-menu-mega-item *ngFor="let sub of item.Items" [item]="sub" [level]="level + 1" [active]="active" (activeChange)="active=$event;activeChange.emit(active);"></d3s-site-menu-mega-item>                
                `,
    changeDetection: ChangeDetectionStrategy.OnPush    
})

export class SiteMenuMegaItemComponent extends BaseComponent implements OnInit{
   
    @Input() item: SiteMenuItem;    
    @Input() level: number;
    @Input() parent: string;
    @Input() active: boolean;
    @Output() activeChange = new EventEmitter();
    count: number;
    numberLoading: boolean;


    constructor(private router: Router, private menuService: SiteMenuService) {
        super();
    }

    ngOnInit(): void {
        this.getItemCount();
    }

    getMargin() {        
        return (this.level * 10) + 'px';
    }

    private getItemCount() {
        if (this.parent) {
            this.numberLoading = true;
            this.menuService.getItemCount(this.parent, this.item.Name).then((result) => { this.count = result; this.numberLoading = false });
        }
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