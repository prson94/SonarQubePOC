import { NgModule } from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule } from '@angular/common';
import { HttpModule, XHRBackend } from '@angular/http';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';

import { AuthenticationConnectionBackend } from '../../authentication-connection-backend';

import { CoreModule } from '../shared/core.module';
import { TilesModule } from '../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';

import { WorkflowMonitorRoutingModule } from './workflowmonitor.routes';
import { WorkflowMonitorComponent } from './workflowmonitor.component';
import { WorkflowMonitorListComponent } from './worflowmonitor-list.component';
import { WorkflowMonitorListFilterComponent } from './workflowmonitor-list-filter.component';
import { WorkflowMonitorListColumnFilterComponent } from './workflowmonitor-list-column-filter.components';
import { WorkflowMonitorStepListComponent } from './workflowmonitor-step-list.component';
import { WorkflowMonitorStepDetailsComponent } from './workflowmonitor-step-details.component';
import { WorkflowMonitorStepFormDetailsComponent } from './workflowmonitor-step-form-details.component';
import { WorkflowMonitorStepEmailDetailsComponent } from './workflowmonitor-step-email-details.component';


import { SharedObjectDetailsModule } from '../shared/objectdetails/shared-object-details.module';

import {
    GrowlModule,
    DataTableModule,
    SharedModule,
    DropdownModule,
    SelectButtonModule,
    MultiSelectModule,
    InputTextModule,
    TooltipModule,
    ButtonModule,
} from 'primeng/primeng';





@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        HttpModule,
        RouterModule,
        FormsModule,

        //primeng
        GrowlModule,
        DataTableModule,
        SharedModule,
        MultiSelectModule,
        DropdownModule,
        SelectButtonModule,
        InputTextModule,
        TooltipModule,
        ButtonModule,

        WorkflowMonitorRoutingModule,

        //d3s        
        CoreModule,
        TilesModule,
        SharedGridPagingInfoModule,        
        SharedObjectDetailsModule
    ],
    declarations: [
        WorkflowMonitorComponent,   
        WorkflowMonitorListComponent,
        WorkflowMonitorListFilterComponent,
        WorkflowMonitorListColumnFilterComponent,
        WorkflowMonitorComponent,    
        WorkflowMonitorStepListComponent,
        WorkflowMonitorStepDetailsComponent,
        WorkflowMonitorStepFormDetailsComponent,
        WorkflowMonitorStepEmailDetailsComponent,
    ],
    exports: [
        WorkflowMonitorComponent,        
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class WorkflowMonitorModule { }