import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernPostRequestInterceptor } from "../../../http-interceptors/govern-post-request.interceptor";

import { RouterModule } from '@angular/router';

import {
    SharedModule,
} from 'primeng/primeng';

import { TableModule } from 'primeng/table';

import { CoreModule } from '../../shared/core.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { TilesModule  } from '../../shared/tiles/tiles.module';

import { AuditRoutingModule } from './audit.routes';

import { AuditComponent } from './audit.component';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,
        RouterModule,

        //routing 
        AuditRoutingModule,

        //d3s        
        CoreModule,
        SharedGridPagingInfoModule,
        TilesModule,

        //prime        
        SharedModule,
        TableModule,
    ],
    declarations: [
        AuditComponent,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernPostRequestInterceptor,
            multi: true },
    ]
})
export class AuditModule { }