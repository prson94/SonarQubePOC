import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';


import { RouterModule } from '@angular/router';

import { SharedModule } from 'primeng/api';

import { TableModule } from 'primeng/table';

import { CoreModule } from '../../shared/core.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { TilesModule } from '../../shared/tiles/tiles.module';

import { FollowersRoutingModule } from './followers.routes';

import { FollowersComponent } from './followers.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,

        RouterModule,

        //routing 
        FollowersRoutingModule,

        //d3s        
        CoreModule,
        SharedGridPagingInfoModule,
        TilesModule,

        //prime        
        SharedModule,
        TableModule,
    ],
    declarations: [
        FollowersComponent,
    ],
    providers: [

    ]
})
export class FollowersModule { }