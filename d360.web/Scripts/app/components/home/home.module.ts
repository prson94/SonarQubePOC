import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';


import { RouterModule } from '@angular/router';

import { CoreModule } from '../shared/core.module';
import { SocialModule } from '../shared/social/social.module';
import { SearchModule } from '../search/search.module';
import { TilesModule } from '../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { ShortcutDisplayModule } from '../shared/shortcutdisplay/shortcut-display.module';
import { DashboardModule } from '../sidebar/dashboard/dashboard.module';

import { HomeComponent } from './home.component';
import { ActivityTile } from './activity-tile.component';
import { ActivityDetailsTile } from './activity-details-tile.component';
import { BoardTile } from './board-tile.component';
import { HomeRoutingModule } from './home.routes';

import { ButtonModule } from 'primeng/button';
import { SharedModule } from 'primeng/api';
import { InputTextModule } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { IgColorPickerModule } from '../shared/controls/color-picker/color-picker.module';
import { UserAssignmentsModule } from '../assignments/user-assignments/user-assignments.module';
import { SharedAssignmentsModule } from '../shared/assignments/shared-assignments.module';
import { SessionTimeoutModalComponent } from './session-timeout-modal/session-timeout-modal.component';
import { SiteModalModule } from '../shared/modal/gov-modal.module';


@NgModule({
    imports: [
        CommonModule,

        RouterModule,

        HomeRoutingModule,

        //primeng  
        InputTextModule,              
        ButtonModule,        
        SharedModule,
        TableModule,

        //d3s
        CoreModule,
        SearchModule,
        IgColorPickerModule,
		SocialModule,
		UserAssignmentsModule,
		SharedAssignmentsModule,
        TilesModule,
        SharedGridPagingInfoModule, 
        ShortcutDisplayModule,
		DashboardModule,
		SiteModalModule
    ],
    declarations: [
        ActivityDetailsTile,
        ActivityTile,
        BoardTile,
        HomeComponent,
        SessionTimeoutModalComponent,
    ],
    providers: [

    ]
})
export class HomeModule { }