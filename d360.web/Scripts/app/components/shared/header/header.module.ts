import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';


import { RouterModule } from '@angular/router';

import { AutoCompleteModule } from 'primeng/autocomplete';
import { TreeModule } from 'primeng/tree';
import { OverlayPanelModule } from 'primeng/overlaypanel';
import { DialogModule } from 'primeng/dialog';
import { SharedModule } from 'primeng/api';

import { PipesModule } from '../../../pipes/pipes.module';

import { TooltipModule } from 'primeng/tooltip';

import { HeaderActionsComponent } from './header-actions.component';
import { HeaderBreadcrumbItemComponent } from './header-breadcrumb-item.component';
import { HeaderBreadcrumbComponent } from './header-breadcrumb.component';
import { HeaderFavoritesComponent } from './header-favorites.component';
import { HeaderFollowComponent } from './header-follow.component';
import { HeaderHelpComponent } from './header-help.component';
import { HeaderComponent } from './header.component';
import { RaiseIssueButtonComponent } from './raise-issue-button.component';
import { HeaderShoppingCartComponent } from './header-shopping-cart.component';
import { HeaderHomePageComponent } from './header-homepage.component';
import { HeaderProfileComponent } from './header-profile.component';
import { HeaderMiniMenuComponent } from './header-mini-menu-component';
import { TypeaheadSearchModule } from '../search/typeahead-search.component';
import { SiteModalModule } from '../modal/gov-modal.module';
import { CoreModule } from "../../shared/core.module";
import { ResourceApiKeyModule } from '../../resource/api-key/resource-api.module';
import { HeaderBackButtonComponent } from './header-back-button.component';


@NgModule({
    imports: [
        CommonModule,
        FormsModule,

        RouterModule,

        //d3s
        CoreModule,
        PipesModule,
        TypeaheadSearchModule,

        //primeng        
        AutoCompleteModule,        
        OverlayPanelModule,
        SharedModule,        
        TreeModule, 
        DialogModule,
        SiteModalModule,
        ResourceApiKeyModule,

        TooltipModule
    ],
    declarations: [
        HeaderActionsComponent,
        HeaderBreadcrumbItemComponent,
        HeaderBreadcrumbComponent,
        HeaderHelpComponent,
        HeaderFavoritesComponent,
        HeaderFollowComponent,
        HeaderComponent,
        RaiseIssueButtonComponent,
        HeaderShoppingCartComponent,
        HeaderHomePageComponent,
        HeaderProfileComponent,
        HeaderMiniMenuComponent,
        HeaderBackButtonComponent,
    ],
    exports: [
        HeaderComponent
    ],
    providers: [

    ]
})
export class HeaderModule { }