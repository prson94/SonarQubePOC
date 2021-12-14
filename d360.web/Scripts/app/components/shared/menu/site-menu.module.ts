import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";

import { RouterModule } from '@angular/router';

import { SiteMenuComponent } from './site-menu.component';
import { SiteMenuMegaItemComponent } from './site-menu-mega-item.component';
import { SiteMenuFavoriteItemComponent } from './site-menu-favorite-item.component';
import { SiteMenuCategoryComponent } from './site-menu-category.component';
import { PipesModule } from '../../../pipes/pipes.module';

import { TooltipModule } from 'primeng/tooltip';
import { DirectivesModule } from '../../../directives/directives.module';
import { SearchFieldModule } from '../controls/search-field/search-field.component';
import { SiteMenuCategoryPanelComponent } from './site-menu-category-panel.component';
import { LinksKeyboardNavigationComponent } from './links-keyboard-navigation.component';
import { SiteMenuFavoritesComponent } from './site-menu-favorites.component';
import { SiteMenuManageFavoritesPanelComponent } from './site-menu-manage-favorites-panel.component';
import { SiteMenuShowFavoritesPanelComponent } from './site-menu-show-favorites-panel.component';
import { CheckboxModule } from 'primeng/checkbox';
import { IgCheckboxModule } from '../../../directives/ig-checkbox-directive';
import { CoreModule } from '../core.module';
import { TriStateCheckboxModule } from 'primeng/tristatecheckbox';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        HttpClientModule,
        RouterModule,
        PipesModule, 

        //prime
        TooltipModule,
        DirectivesModule,
        SearchFieldModule,
        CheckboxModule,
        IgCheckboxModule,
        CoreModule,
        TriStateCheckboxModule
    ],
    declarations: [
        SiteMenuComponent,
        SiteMenuMegaItemComponent,
        SiteMenuCategoryComponent,
        SiteMenuCategoryPanelComponent,
        SiteMenuFavoriteItemComponent,
        SiteMenuFavoritesComponent,
        SiteMenuShowFavoritesPanelComponent,
        SiteMenuManageFavoritesPanelComponent,
        LinksKeyboardNavigationComponent
    ],
    exports: [
        SiteMenuComponent,        
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
    ]
})
export class SiteMenuModule { }