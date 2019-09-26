import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../http-interceptors/govern-request.interceptor";

import { RouterModule } from '@angular/router';

import { CoreModule } from '../shared/core.module';
import { SocialModule } from '../shared/social/social.module';
import { SearchModule } from '../search/search.module';
import { TilesModule  } from '../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { SharedAssignmentsModule } from '../shared/assignments/shared-assignments.module'
import { ShortcutModule } from '../shared/shortcuts/shortcut.module';
import { DashboardModule } from '../sidebar/dashboard/dashboard.module';

import { HomeComponent} from './home.component';
import { ActivityTile } from './activity-tile.component';
import { ActivityDetailsTile } from './activity-details-tile.component';
import { BoardTile} from './board-tile.component';
import { HomeRoutingModule } from './home.routes';

import { ButtonModule } from 'primeng/button';
import { SharedModule } from 'primeng/shared';
import { InputTextModule } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { TooltipModule } from 'primeng/tooltip';
import { GrowlModule } from 'primeng/growl';

@NgModule({
    imports: [CommonModule,     
        DeprecatedI18NPipesModule,
        HttpClientModule,
        RouterModule,

        HomeRoutingModule,

        //primeng  
        InputTextModule,
        GrowlModule,       
        ButtonModule,
        TooltipModule,                     
        SharedModule,
        TableModule,

        //d3s
        CoreModule,
        SearchModule,
        SocialModule,
        SharedAssignmentsModule,
        TilesModule,
        SharedGridPagingInfoModule, 
        ShortcutModule,
        DashboardModule,

    ],
    declarations: [
        ActivityDetailsTile,
        ActivityTile,
        BoardTile,
        HomeComponent,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
    ]
})
export class HomeModule { }