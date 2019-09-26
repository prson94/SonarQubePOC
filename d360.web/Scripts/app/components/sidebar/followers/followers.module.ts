import { NgModule } from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";
import { RouterModule } from '@angular/router';

import { SharedModule } from 'primeng/shared';

import { TableModule } from 'primeng/table';

import { CoreModule } from '../../shared/core.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { TilesModule } from '../../shared/tiles/tiles.module';

import { FollowersRoutingModule } from './followers.routes';

import { FollowersComponent } from './followers.component';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,
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
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
    ]
})
export class FollowersModule { }