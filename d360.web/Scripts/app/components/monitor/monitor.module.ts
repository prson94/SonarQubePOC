import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';

import { AuthenticationConnectionBackend } from '../../authentication-connection-backend';

import { CoreModule } from '../shared/core.module';
import { TilesModule  } from '../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';

import { MonitorRoutingModule } from './monitor.routes';
import { MonitorComponent } from './monitor.component';
import { MonitorFilterComponent } from './monitor-filter.component';
import { MonitorWorkflowComponent } from './monitor-workflow.component';
import { MonitorWorkflowItemComponent } from './monitor-workflow-item.component';

import { MonitorListComponent } from './monitor-list.component';

import { WorkflowDiagramModule } from '../shared/diagram/workflow/workflow-diagram.module';


import {
    GrowlModule,
    DataTableModule,
    SharedModule,
    DropdownModule,
    MultiSelectModule,
    InputTextModule,
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,        
        HttpModule,
        RouterModule,
        FormsModule,

        //primeng
        GrowlModule,
        DataTableModule,
        SharedModule,
        MultiSelectModule,
        DropdownModule,
        InputTextModule,

        MonitorRoutingModule,

        //d3s        
        CoreModule,        
        TilesModule,
        SharedGridPagingInfoModule,
        WorkflowDiagramModule,
    ],
    declarations: [   
        MonitorComponent,
        MonitorFilterComponent,     
        MonitorListComponent, 
        MonitorWorkflowComponent,    
        MonitorWorkflowItemComponent,   
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ] 
})
export class MonitorModule { }