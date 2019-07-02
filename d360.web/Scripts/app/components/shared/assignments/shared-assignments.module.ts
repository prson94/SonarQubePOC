import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernPostRequestInterceptor } from "../../../http-interceptors/govern-post-request.interceptor";

import {
    SharedModule,
} from 'primeng/primeng';

import { TableModule } from 'primeng/table';

import { CoreModule } from '../core.module';
import { TilesModule  } from '../tiles/tiles.module';

import { AssignmentsComponent } from './assignments.component';
import { SharedGridPagingInfoModule } from '../grid-paging-info.component';


@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        HttpClientModule,

        //d3s
        CoreModule,        
        TilesModule,
        SharedGridPagingInfoModule,

        //prime        
        SharedModule,
        TableModule,
    ],
    declarations: [
        AssignmentsComponent
    ],
    exports: [
        AssignmentsComponent
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernPostRequestInterceptor,
            multi: true },
    ]
})
export class SharedAssignmentsModule { }