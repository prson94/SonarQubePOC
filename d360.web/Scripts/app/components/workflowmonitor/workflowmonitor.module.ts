import { NgModule } from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule } from '@angular/common';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../http-interceptors/govern-request.interceptor";

import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';

import { CoreModule } from '../shared/core.module';
import { TilesModule } from '../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { PipesModule } from '../../pipes/pipes.module';

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

import { SharedModule } from 'primeng/shared';
import { GrowlModule } from 'primeng/growl';
import { CalendarModule } from 'primeng/calendar';
import { InputTextModule } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { MultiSelectModule } from 'primeng/multiselect';
import { DropdownModule } from 'primeng/dropdown';
import { SelectButtonModule } from 'primeng/selectbutton';
import { TooltipModule } from 'primeng/tooltip';


@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        HttpClientModule,
        RouterModule,
        FormsModule,
        PipesModule,

        //primeng
        GrowlModule,
        SharedModule,
        MultiSelectModule,
        DropdownModule,
        SelectButtonModule,
        InputTextModule,
        TooltipModule,
        ButtonModule,
        CalendarModule,
        TableModule,

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
        WorkflowMonitorListComponent,
        WorkflowMonitorStepListComponent,
        WorkflowMonitorStepDetailsComponent,

    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
    ]
})
export class WorkflowMonitorModule { }
