import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule }     from '@angular/http';
import { RouterModule } from '@angular/router';

import {    
    AutoCompleteModule,    
    TreeModule,
    OverlayPanelModule,
    SharedModule
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


@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,

        //d3s
        PipesModule,

        //primeng        
        AutoCompleteModule,        
        SharedModule,        
        TreeModule,
        OverlayPanelModule,
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
    ],
    exports: [
        HeaderComponent
    ]
})
export class HeaderModule { }