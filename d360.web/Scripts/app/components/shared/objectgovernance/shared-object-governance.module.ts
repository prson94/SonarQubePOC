import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { RouterModule }    from '@angular/router';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";

import { ButtonModule } from 'primeng/button';
import { SharedModule } from 'primeng/api';
import { TreeTableModule } from 'primeng/treetable';

import { ChartModule } from 'angular2-highcharts';

import { CoreModule } from '../core.module';
import { TilesModule  } from '../tiles/tiles.module';
import { SocialModule } from '../social/social.module';
import { WorkflowModule } from '../../workflow/workflow.module';
import { SharedGridPagingInfoModule } from '../grid-paging-info.component';

import { ObjectBoardComponent } from './object-board.component';
import { ObjectHealthDetailsComponent } from './object-health-details.component';
import { ObjectIssuesComponent } from './object-issues.component';
import { HighchartsStatic } from 'angular2-highcharts/dist/HighchartsService';
import { PipesModule } from '../../../pipes/pipes.module';
import { ObjectHealthDetailsItemComponent } from './object-health-details-item.component';
import { SimpleCarouselModule } from '../small-widgets/carausel/simple-carousel.module';
import { TooltipModule } from 'primeng/tooltip';

declare var require: any;
export function highchartsFactory() {
    const hc = require('highcharts');
    const hcm = require('highcharts/highcharts-more'); // used for more category of charts        
    hcm(hc);        
    return hc;
}

@NgModule({
    imports: [
        CommonModule,
        RouterModule,
        HttpClientModule,
        //d3s
        CoreModule,
        SharedGridPagingInfoModule,        
        SocialModule,
        TilesModule,
        WorkflowModule,
        PipesModule,
        //prime        
        ButtonModule,
        SharedModule,  
        TreeTableModule,
        TooltipModule,

        //charts        
        ChartModule,
        SimpleCarouselModule
    ],
    declarations: [    
        ObjectBoardComponent,
        ObjectHealthDetailsComponent,        
        ObjectIssuesComponent,
        ObjectHealthDetailsItemComponent,
    ],
    exports: [
        ObjectBoardComponent,                
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