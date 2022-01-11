import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';


import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';

import { CoreModule } from '../shared/core.module';
import { TilesModule  } from '../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';

import { MonitorRoutingModule } from './monitor.routes';
import { MonitorComponent } from './monitor.component';
import { MonitorFilterComponent } from './monitor-filter.component';
import { MonitorListComponent } from './monitor-list.component';

import { WorkflowMonitorModule } from '../workflowmonitor/workflowmonitor.module';
import { WorkflowDiagramModule } from '../shared/diagram/workflow/workflow-diagram.module';
import { SharedObjectDetailsModule } from '../shared/objectdetails/shared-object-details.module';

import { TabViewModule } from 'primeng/tabview';
import { ButtonModule } from 'primeng/button';
import { SharedModule } from 'primeng/api';
import { InputTextModule } from 'primeng/inputtext';
import { DropdownModule } from 'primeng/dropdown';
import { TableModule } from 'primeng/table';
import { MultiSelectModule } from 'primeng/multiselect';
import { TooltipModule } from 'primeng/tooltip';
import { ToastModule } from 'primeng/toast';

import { MonitorWorkflowVersionComponent } from './monitor-workflow-version.component';
import { SharedWorkflowMonitorModule } from '../shared/workflow/shared-workflow.module';

@NgModule({
    imports: [
        CommonModule,

        RouterModule,
        FormsModule,

        //primeng
        ToastModule,
        SharedModule,
        MultiSelectModule,
        DropdownModule,
        InputTextModule,
        TooltipModule,
        ButtonModule,
        TableModule,
        MonitorRoutingModule,
        TabViewModule,
        SharedWorkflowMonitorModule,

        //d3s        
        CoreModule,        
        TilesModule,
        SharedGridPagingInfoModule,
        WorkflowDiagramModule,
        SharedObjectDetailsModule,
        WorkflowMonitorModule,
    ],
    declarations: [   
        MonitorComponent,
        MonitorFilterComponent,     
        MonitorListComponent,    
        MonitorWorkflowVersionComponent,
    ],
    exports: [
        MonitorComponent,
        MonitorFilterComponent,
        MonitorListComponent,
        ],
    providers: [

    ] 
})
export class MonitorModule { }