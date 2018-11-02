import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../authentication-connection-backend';

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
import { D3SSortIconModule } from '../shared/turbotable-sorticon.component';
import { D3SColumnFilterModule } from '../shared/turbotable-column-filter.component';
import { HomeRoutingModule } from './home.routes';

import {
    GrowlModule,
    DataTableModule,
    ButtonModule,
    TooltipModule,    
    SharedModule,
    InputTextModule,
} from 'primeng/primeng';

import { TableModule } from 'primeng/table';

@NgModule({
    imports: [CommonModule,     
        DeprecatedI18NPipesModule,
        HttpModule,
        RouterModule,

        HomeRoutingModule,

        //primeng  
        InputTextModule,
        GrowlModule,       
        DataTableModule,
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
        D3SSortIconModule,
        D3SColumnFilterModule,

    ],
    declarations: [
        ActivityDetailsTile,
        ActivityTile,
        BoardTile,
        HomeComponent,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class HomeModule { }