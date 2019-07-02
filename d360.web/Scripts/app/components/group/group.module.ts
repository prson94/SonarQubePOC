import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernPostRequestInterceptor } from "../../http-interceptors/govern-post-request.interceptor";

import { RouterModule } from '@angular/router';

import { CoreModule } from '../shared/core.module';
import { D3SSharedModule } from '../shared/shared.module';
import { PipesModule } from '../../pipes/pipes.module';
import { TilesModule  } from '../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';

import { GroupRoutingModule } from './group.routes';

import { GroupComponent } from './group.component';
import { GroupItemComponent } from './group-item.component';
import { GroupListComponent } from './group-list.component';
import { GroupResponsibilityComponent } from './group-responsibility.component';

import {
    GrowlModule,    
    SharedModule,
} from 'primeng/primeng';

import { TableModule } from 'primeng/table';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,
        RouterModule,

        GroupRoutingModule,

        //primeng
        GrowlModule,        
        SharedModule,
        TableModule,

        //d3s
        D3SSharedModule,        
        CoreModule,
        PipesModule,
        TilesModule,
        SharedGridPagingInfoModule,
    ],
    declarations: [
        GroupComponent,
        GroupItemComponent,
        GroupListComponent,
        GroupResponsibilityComponent,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernPostRequestInterceptor,
            multi: true },
    ]
})
export class GroupModule { }