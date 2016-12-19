import { Input, Component, Output, EventEmitter, ChangeDetectionStrategy} from '@angular/core';
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
                    <i [class]="'fa fa-circle menu-level-indicator-' + level" aria-hidden="true"></i>{{item.Name}}</a>                    
                <d3s-site-menu-mega-item *ngFor="let sub of item.Items" [item]="sub" [level]="level + 1" [active]="active" (activeChange)="active=$event;activeChange.emit(active);"></d3s-site-menu-mega-item>                
                `,
    changeDetection: ChangeDetectionStrategy.OnPush    
})

export class SiteMenuMegaItemComponent extends BaseComponent {
    @Input() item: SiteMenuItem;    
    @Input() level: number;

    @Input() active: boolean;
    @Output() activeChange = new EventEmitter();
    
    constructor(private router: Router) {
        super();
    }

    getMargin() {        
        return (this.level * 10) + 'px';
    }

    itemClick() {
        if (this.item.IsLink)
            window.location.href = this.item.Url;
        else
            this.router.navigateByUrl(this.item.Url);

        this.active = false;
        this.activeChange.emit(this.active);
    }    
};