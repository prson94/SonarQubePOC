import { NgModule } from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpModule, XHRBackend } from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

import {
    DataTableModule,
    SharedModule,
    ButtonModule,
} from 'primeng/primeng';

import { CoreModule } from '../../shared/core.module';
import { TilesModule } from '../../shared/tiles/tiles.module';
import { WorkflowDiagramModule } from '../../shared/diagram/workflow/workflow-diagram.module';

import { WorkflowMonitorRoutingModule } from './workflow-monitor.routes';

import { WorkflowMonitorComponent } from './workflow-monitor.component';
import { MonitorWorkflowComponent } from './monitor-workflow.component';

import { MonitorModule } from '../../monitor/monitor.module';


@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpModule,
        RouterModule,

        //routing 
        WorkflowMonitorRoutingModule,

        //d3s        
        CoreModule,        
        TilesModule,
        WorkflowDiagramModule,
        MonitorModule,

        //prime        
        DataTableModule,
        SharedModule,
        ButtonModule,
    ],
    declarations: [
        WorkflowMonitorComponent,
        MonitorWorkflowComponent,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class WorkflowMonitorModule { }