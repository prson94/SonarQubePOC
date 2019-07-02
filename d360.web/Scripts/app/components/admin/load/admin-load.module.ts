import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernPostRequestInterceptor } from "../../../http-interceptors/govern-post-request.interceptor";

import { RouterModule } from '@angular/router';

import { CoreModule } from '../../shared/core.module';
import { PipesModule } from '../../../pipes/pipes.module';
import { TilesModule  } from '../../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../../shared/delete.form';
import { SharedObjectDetailsModule } from '../../shared/objectdetails/shared-object-details.module';

import { BulkLoadItemComponent } from './bulk-load-item.component';
import { LoadForm } from './load.form';
import { AdminLoadComponent } from './admin-load.component';

import { AdminLoadRoutingModule } from './admin-load.routes';

import {
    ButtonModule,
    DropdownModule,
    InputTextModule,
    SharedModule,
} from 'primeng/primeng';

import { TableModule } from 'primeng/table';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,

        AdminLoadRoutingModule,

        //prime
        ButtonModule,
        DropdownModule,
        InputTextModule,
        SharedModule,
        TableModule,

        //d3s        
        CoreModule,
        PipesModule,        
        SharedDeleteFormModule,                
        SharedGridPagingInfoModule,
        SharedObjectDetailsModule,
        TilesModule,
    ],
    declarations: [
        BulkLoadItemComponent,
        LoadForm,
        AdminLoadComponent,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernPostRequestInterceptor,
            multi: true },
    ]
})
export class AdminLoadModule { }