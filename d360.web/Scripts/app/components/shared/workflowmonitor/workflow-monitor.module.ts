import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { HttpModule, XHRBackend } from '@angular/http';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

import {
    DataTableModule,    
    SharedModule,    
    ButtonModule,
} from 'primeng/primeng';

import { CoreModule } from '../core.module';
import { TilesModule } from '../tiles/tiles.module';
import { WorkflowDiagramModule } from '../diagram/workflow/workflow-diagram.module';

import { WorkflowMonitorComponent } from './workflow-monitor.component';


@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,
        //d3s
        CoreModule,
        WorkflowDiagramModule,
        TilesModule,
        

        //prime        
        DataTableModule,        
        SharedModule,
        ButtonModule,
    ],
    declarations: [
        WorkflowMonitorComponent,
    ],
    exports: [
        WorkflowMonitorComponent,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class WorkflowMonitorModule { }