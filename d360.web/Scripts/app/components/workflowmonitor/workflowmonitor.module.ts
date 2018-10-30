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
import { WorkflowMonitorStepGridComponent } from './workflowmonitor-step-grid.component';
import { WorkflowMonitorStepDetailsComponent } from './workflowmonitor-step-details.component';
import { WorkflowMonitorStepFormDetailsComponent } from './workflowmonitor-step-form-details.component';
import { WorkflowMonitorStepEmailDetailsComponent } from './workflowmonitor-step-email-details.component';
import { WorkflowMonitorActionDetailsComponent } from './workflowmonitor-action-details.component';
import { WorkflowMonitorStepFieldChangeDetailsComponent } from './workflowmonitor-step-field-change-details.component';
import { WorkflowMonitorStepRelationshipChangeDetailsComponent } from './workflowmonitor-step-relationship-change-details';


import { SharedObjectDetailsModule } from '../shared/objectdetails/shared-object-details.module';
import { D3SSharedModule } from '../shared/shared.module';
import { SimpleAccordionModule } from '../shared/simple-accordion.part';

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
    CalendarModule,
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
        CalendarModule,

        WorkflowMonitorRoutingModule,

        //d3s        
        CoreModule,
        TilesModule,
        SharedGridPagingInfoModule,        
        SharedObjectDetailsModule,
        D3SSharedModule,
        SimpleAccordionModule,
    ],
    declarations: [
        WorkflowMonitorComponent,   
        WorkflowMonitorListComponent,
        WorkflowMonitorListFilterComponent,
        WorkflowMonitorListColumnFilterComponent,
        WorkflowMonitorComponent,   
        WorkflowMonitorStepGridComponent,
        WorkflowMonitorStepListComponent,
        WorkflowMonitorStepDetailsComponent,
        WorkflowMonitorStepFormDetailsComponent,
        WorkflowMonitorStepEmailDetailsComponent,
        WorkflowMonitorActionDetailsComponent,
        WorkflowMonitorStepFieldChangeDetailsComponent,
        WorkflowMonitorStepRelationshipChangeDetailsComponent,
    ],
    exports: [
        WorkflowMonitorComponent,        
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class WorkflowMonitorModule { }