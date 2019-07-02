import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernPostRequestInterceptor } from "../../http-interceptors/govern-post-request.interceptor";
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

import {
    GrowlModule,
    SharedModule,
    DropdownModule,
    MultiSelectModule,
    InputTextModule,
    TooltipModule,
    ButtonModule,
    TabViewModule,
} from 'primeng/primeng';
import { TableModule } from 'primeng/table';
import { MonitorWorkflowVersionComponent } from './monitor-workflow-version.component';

@NgModule({
    imports: [CommonModule,   
        DeprecatedI18NPipesModule,
        HttpClientModule,
        RouterModule,
        FormsModule,

        //primeng
        GrowlModule,
        SharedModule,
        MultiSelectModule,
        DropdownModule,
        InputTextModule,
        TooltipModule,
        ButtonModule,
        TableModule,
        MonitorRoutingModule,
        TabViewModule,

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
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernPostRequestInterceptor,
            multi: true },
    ] 
})
export class MonitorModule { }