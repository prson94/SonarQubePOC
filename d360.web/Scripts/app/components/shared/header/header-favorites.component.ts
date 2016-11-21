import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, OnChanges, SimpleChange } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { FavoritesService, MessagesService } from '../../../services/index';
import { Favorite } from '../../../models/favorite.model';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { HeaderActionsService } from '../../../services/header-actions.service';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import * as _ from 'lodash';


@Component({
    selector: 'd3s-header-favorites',    
    template:
    `
        <span (click)="handleClick()" class="favorite" [ngClass]="{'active':isFavoriteItem}" [title]="isFavoriteItem ? 'Remove from favorites' : 'Add to favorites'" >
            <i *ngIf="!isLoading" class="fa fa-star"></i>            
        </span>
    `,
    providers: [FavoritesService]
})

export class HeaderFavoritesComponent implements OnInit, OnDestroy, OnChanges {
    @Input() uri: string;
    @Input() isFavoriteItem: boolean = false;
    
    private subObjectChange: any;    
    private subFavorites: any;  
    private subBreadcrumb: any;  
    private isLoading = false;

    private favItems: Favorite[];

    private currentObject: string;
    private currentObjectId: number;
    private name: string;
    
    constructor(private router: Router,
        private messagesService: MessagesService,
        private favoritesService: FavoritesService,
        private breadcrumbService: HeaderBreadcrumbService,
        protected headerActionsService: HeaderActionsService) {        
    }
    
    ngOnInit() {        
        this.subObjectChange = this.breadcrumbService.currentObjectInfo$.subscribe(c => {            
            this.currentObject = c.type;
            this.currentObjectId = c.id;            
            if (this.favItems == null) {
                this.favoritesService.getFavorites()
                    .then(fav => {
                        this.favItems = fav;
                        this.checkIsFavorite();
                    });
            } else {
                this.checkIsFavorite();
            }
        });

        this.subBreadcrumb = this.breadcrumbService.breadcrumbs$.subscribe(b => {
            this.name = b.text;            
        });
        
        this.subFavorites = this.headerActionsService.onFavoritesChanges$.subscribe(() => {        
            this.favoritesService.getFavorites().then(res => {
                this.favItems = res;
                this.checkIsFavorite();
            });
        });
        
        this.favoritesService.getFavorites()
            .then(fav => {
                this.favItems = fav;    
                this.checkIsFavorite();       
            });
    }
    
    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.uri && changes["uri"]) {
            this.checkIsFavorite();
        }
    }

    handleClick() {
        if (this.isLoading) {
            console.log('ERROR: CANNOT SAVE FAVORITE LOADING');
            return;
        }

        if (this.isAdminUri()) {
            console.log('ERROR: CANNOT SAVE FAVORITE FOR ADMIN PAGES');
            return;
        }
        this.isLoading = true;
        let f = new Favorite();
        f.ObjectID = this.currentObjectId;
        f.Object = this.currentObject;
        f.Name = this.name;
        f.Route = this.uri ? this.uri : 'home';//null route is home        
        this.isFavoriteItem = !this.isFavoriteItem;
        this.favoritesService.toggleFavorite(f)
            .then(fav => {                
                this.headerActionsService.emitFavoritesChange();                
                this.isLoading = false;
            });            
    }
    
    checkIsFavorite() {        
        if (this.favItems == null) return;

        this.isFavoriteItem = false;
        if (!this.uri) this.uri = 'home';
        let index = this.favItems.findIndex(x => x.Route == this.uri);
        
        this.isFavoriteItem = index >= 0;        
    }

    ngOnDestroy() {
        this.subObjectChange.unsubscribe();        
        this.subFavorites.unsubscribe();
        this.subBreadcrumb.unsubscribe();
    }

    isAdminUri() {                
        return (this.uri || '').toUpperCase().startsWith(SiteUrlHelpers.SITE_URL_ADMIN_ROOT.toUpperCase());
    }
}

