import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";
import { RouterModule } from '@angular/router';

import { AutoCompleteModule } from 'primeng/autocomplete';
import { TreeModule } from 'primeng/tree';
import { OverlayPanelModule } from 'primeng/overlaypanel';
import { DialogModule } from 'primeng/dialog';
import { SharedModule } from 'primeng/shared';

import { PipesModule } from '../../../pipes/pipes.module';

import { HeaderActionsComponent } from './header-actions.component';
import { HeaderBreadcrumbItemComponent } from './header-breadcrumb-item.component';
import { HeaderBreadcrumbComponent } from './header-breadcrumb.component';
import { HeaderTypeaheadSearchComponent } from './header-typeahead-search.component';
import { HeaderFavoritesComponent } from './header-favorites.component';
import { HeaderFollowComponent } from './header-follow.component';
import { HeaderHelpComponent } from './header-help.component';
import { HeaderComponent } from './header.component';
import { RaiseIssueButtonComponent } from './raise-issue-button.component';
import { HeaderShoppingCartComponent } from './header-shopping-cart.component';
import { HeaderHomePageComponent } from './header-homepage.component';
import { HeaderProfileComponent } from './header-profile.component';
import { HeaderMiniMenuComponent } from './header-mini-menu-component';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,
        RouterModule,

        //d3s
        PipesModule,

        //primeng        
        AutoCompleteModule,        
        OverlayPanelModule,
        SharedModule,        
        TreeModule, 
        DialogModule,
    ],
    declarations: [
        HeaderActionsComponent,
        HeaderBreadcrumbItemComponent,
        HeaderBreadcrumbComponent,
        HeaderTypeaheadSearchComponent,
        HeaderHelpComponent,
        HeaderFavoritesComponent,
        HeaderFollowComponent,
        HeaderComponent,
        RaiseIssueButtonComponent,
        HeaderShoppingCartComponent,
        HeaderHomePageComponent,
        HeaderProfileComponent,
        HeaderMiniMenuComponent,
    ],
    exports: [
        HeaderComponent
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
    ]
})
export class HeaderModule { }