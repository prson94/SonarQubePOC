import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { RouterModule }    from '@angular/router';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";

import {
    ButtonModule,
    SharedModule,   
    TreeTableModule,
} from 'primeng/primeng';

import { ChartModule } from 'angular2-highcharts';

import { CoreModule } from '../core.module';
import { TilesModule  } from '../tiles/tiles.module';
import { SocialModule } from '../social/social.module';
import { WorkflowModule } from '../../workflow/workflow.module';
import { SharedGridPagingInfoModule } from '../grid-paging-info.component';

import { ArtifactStatusComponent } from './artifact-status.component';
import { ObjectBoardComponent } from './object-board.component';
import { ObjectGovernanceComponent } from './object-governance.component';
import { ObjectHealthDetailsComponent } from './object-health-details.component';
import { ObjectHealthComponent } from './object-health.component';
import { ObjectIssuesComponent } from './object-issues.component';
import { HighchartsStatic } from 'angular2-highcharts/dist/HighchartsService';

declare var require: any;
export function highchartsFactory() {
    const hc = require('highcharts');
    const hcm = require('highcharts/highcharts-more'); // used for more category of charts    
    const solidGauge = require('highcharts/modules/solid-gauge');
    hcm(hc);    
    solidGauge(hc);
    return hc;
}

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        RouterModule,
        HttpClientModule,
        //d3s
        CoreModule,
        SharedGridPagingInfoModule,        
        SocialModule,
        TilesModule,
        WorkflowModule,
        //prime        
        ButtonModule,
        SharedModule,  
        TreeTableModule,

        //charts        
        ChartModule,
    ],
    declarations: [    
        ArtifactStatusComponent,
        ObjectBoardComponent,
        ObjectGovernanceComponent,
        ObjectHealthDetailsComponent,
        ObjectHealthComponent,
        ObjectIssuesComponent,
    ],
    exports: [
        ObjectBoardComponent,        
        ObjectGovernanceComponent,
        ObjectHealthComponent,
        ObjectHealthDetailsComponent,     
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
        {
            provide: HighchartsStatic,
            useFactory: highchartsFactory
        },
    ]
})
export class SharedObjectGovernanceModule { }