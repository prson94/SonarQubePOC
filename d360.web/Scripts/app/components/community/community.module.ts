import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../authentication-connection-backend';

import { ChartModule } from 'angular2-highcharts';

import { CoreModule } from '../shared/core.module';
import { D3SSharedModule } from '../shared/shared.module';
import { PipesModule } from '../../pipes/pipes.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';

import { CommunityComponent } from './community.component';
import { CommunityResponsibilityCountComponent } from './community-responsibility-count.component';

import { CommunityRoutingModule } from './community.routes';

import {
    SharedModule,
    DataTableModule,
    GrowlModule
} from 'primeng/primeng';

import { TableModule } from 'primeng/table';

import { HighchartsStatic } from 'angular2-highcharts/dist/HighchartsService';

declare var require: any;
export function highchartsFactory() {
    const highcharts = require('highcharts');

    ChartModule.forRoot(require('highcharts'));
    return highcharts;
}

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpModule,
        RouterModule,

        CommunityRoutingModule,

        //highcharts        
        ChartModule,

        //prime
        SharedModule,
        DataTableModule,
        GrowlModule,
        TableModule,

        //d3s
        D3SSharedModule,
        CoreModule,
        PipesModule,
        SharedGridPagingInfoModule,
    ],
    declarations: [
        CommunityComponent,
        CommunityResponsibilityCountComponent,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
        {
            provide: HighchartsStatic,
            useFactory: highchartsFactory
        },
    ]
})
export class CommunityModule { }