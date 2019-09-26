import { NgModule } from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from '../../../http-interceptors/govern-request.interceptor';

import { RouterModule } from '@angular/router';

import { ButtonModule } from 'primeng/button';
import { SharedModule } from 'primeng/shared';

import { TableModule } from 'primeng/table';

import { CoreModule } from '../../shared/core.module';
import { TilesModule } from '../../shared/tiles/tiles.module';
import { WorkflowDiagramModule } from '../../shared/diagram/workflow/workflow-diagram.module';

import { WorkflowMonitorRoutingModule } from './workflow-monitor.routes';

import { MonitorWorkflowComponent } from './monitor-workflow.component';

import { MonitorModule } from '../../monitor/monitor.module';


@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,
        RouterModule,

        //routing 
        WorkflowMonitorRoutingModule,

        //d3s        
        CoreModule,        
        TilesModule,
        WorkflowDiagramModule,
        MonitorModule,

        //prime        
        SharedModule,
        ButtonModule,
        TableModule,
    ],
    declarations: [
        MonitorWorkflowComponent,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
    ]
})
export class WorkflowMonitorModule { }