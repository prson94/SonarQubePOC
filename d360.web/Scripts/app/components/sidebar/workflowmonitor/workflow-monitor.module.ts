import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { GovernRequestInterceptor } from '../../../http-interceptors/govern-request.interceptor';

import { RouterModule } from '@angular/router';

import { ButtonModule } from 'primeng/button';
import { SharedModule } from 'primeng/api';

import { TableModule } from 'primeng/table';

import { CoreModule } from '../../shared/core.module';
import { TilesModule } from '../../shared/tiles/tiles.module';
import { WorkflowDiagramModule } from '../../shared/diagram/workflow/workflow-diagram.module';

import { WorkflowMonitorRoutingModule } from './workflow-monitor.routes';

import { MonitorWorkflowComponent } from './monitor-workflow.component';

import { MonitorModule } from '../../monitor/monitor.module';


@NgModule({
    imports: [
        CommonModule,
        FormsModule,

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

    ]
})
export class WorkflowMonitorModule { }