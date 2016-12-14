import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../authentication-connection-backend';

import { CoreModule } from '../shared/core.module';
import { SocialModule } from '../shared/social/social.module';
import { SearchModule } from '../search/search.module';
import { TilesModule  } from '../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { SharedDashboardModule } from '../shared/dashboard/shared-dashboard.module'
import { SharedAssignmentsModule } from '../shared/assignments/shared-assignments.module'

import { HomeComponent} from './home.component';
import { ActivityTile } from './activity-tile.component';
import { ActivityDetailsTile } from './activity-details-tile.component';
import { BoardTile} from './board-tile.component';

import { HomeRoutingModule } from './home.routes';

import {
    GrowlModule,
    InputTextModule,
    DataTableModule,
    ButtonModule,
    DropdownModule,
    CheckboxModule,        
    MultiSelectModule,
    TooltipModule,    
    SharedModule,
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,        
        HttpModule,
        RouterModule,

        HomeRoutingModule,

        //primeng  
        GrowlModule,
        InputTextModule,        
        DataTableModule,
        ButtonModule,
        DropdownModule,
        CheckboxModule,                
        MultiSelectModule,        
        TooltipModule,                     
        SharedModule,

        //d3s        
        CoreModule,
        SearchModule,
        SocialModule,
        SharedAssignmentsModule,
        TilesModule,
        SharedGridPagingInfoModule,
        SharedDashboardModule,
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