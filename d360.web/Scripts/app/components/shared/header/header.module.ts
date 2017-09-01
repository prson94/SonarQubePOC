import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

import {    
    AutoCompleteModule,    
    TreeModule,
    OverlayPanelModule,
    SharedModule,    
} from 'primeng/primeng';

import { PipesModule } from '../../../pipes/pipes.module';

import { HeaderActionsComponent } from './header-actions.component';
import { HeaderBreadcrumbItemComponent } from './header-breadcrumb-item.component';
import { HeaderBreadcrumbComponent } from './header-breadcrumb.component';
import { HeaderTypeaheadSearchComponent } from './header-typeahead-search.component';
import { HeaderFavoritesComponent } from './header-favorites.component';
import { HeaderFollowComponent } from './header-follow.component';
import { HeaderComponent } from './header.component';
import { RaiseIssueButtonComponent } from './raise-issue-button.component';
import { HeaderShoppingCartComponent } from './header-shopping-cart.component';
import { HeaderHomePageComponent } from './header-homepage.component';


@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,

        //d3s
        PipesModule,

        //primeng        
        AutoCompleteModule,        
        OverlayPanelModule,
        SharedModule,        
        TreeModule,                
    ],
    declarations: [
        HeaderActionsComponent,
        HeaderBreadcrumbItemComponent,
        HeaderBreadcrumbComponent,
        HeaderTypeaheadSearchComponent,
        HeaderFavoritesComponent,
        HeaderFollowComponent,
        HeaderComponent,
        RaiseIssueButtonComponent,
        HeaderShoppingCartComponent,
        HeaderHomePageComponent,
    ],
    exports: [
        HeaderComponent
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class HeaderModule { }