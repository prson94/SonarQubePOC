import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
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

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,

        //routing 
        WorkflowMonitorRoutingModule,

        //d3s        
        CoreModule,        
        TilesModule,
        WorkflowDiagramModule,

        //prime        
        DataTableModule,
        SharedModule,
        ButtonModule,
    ],
    declarations: [
        WorkflowMonitorComponent,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class WorkflowMonitorModule { }