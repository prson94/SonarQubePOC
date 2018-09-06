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

import { SharedObjectDetailsModule } from '../shared/objectdetails/shared-object-details.module';

import {
    GrowlModule,
    DataTableModule,
    SharedModule,
    DropdownModule,
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
    ],
    exports: [
        WorkflowMonitorComponent,        
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class WorkflowMonitorModule { }