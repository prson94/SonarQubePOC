import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
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

@NgModule({
    imports: [CommonModule,
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
    ]
})
export class CommunityModule { }