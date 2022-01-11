import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';



import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';

import { CoreModule } from '../shared/core.module';
import { TilesModule } from '../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { PipesModule } from '../../pipes/pipes.module';

import { WorkflowMonitorRoutingModule } from './workflowmonitor.routes';
import { WorkflowMonitorComponent } from './workflowmonitor.component';
import { WorkflowMonitorListComponent } from './workflowmonitor-list.component';
import { WorkflowMonitorListFilterComponent } from './workflowmonitor-list-filter.component';
import { WorkflowMonitorListColumnFilterComponent } from './workflowmonitor-list-column-filter.components';

import { SharedObjectDetailsModule } from '../shared/objectdetails/shared-object-details.module';
import { D3SSharedModule } from '../shared/shared.module';
import { SimpleAccordionModule } from '../shared/simple-accordion.part';

import { SharedModule } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { CalendarModule } from 'primeng/calendar';
import { InputTextModule } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { MultiSelectModule } from 'primeng/multiselect';
import { DropdownModule } from 'primeng/dropdown';
import { SelectButtonModule } from 'primeng/selectbutton';
import { TooltipModule } from 'primeng/tooltip';
import { SharedWorkflowMonitorModule } from '../shared/workflow/shared-workflow.module';


@NgModule({
    imports: [
        CommonModule,

        RouterModule,
        FormsModule,
        PipesModule,

        //primeng
        ToastModule,
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
        SharedWorkflowMonitorModule,

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
    ],
    exports: [
        WorkflowMonitorComponent,  
        WorkflowMonitorListComponent,

    ],
    providers: [

    ]
})
export class WorkflowMonitorModule { }
