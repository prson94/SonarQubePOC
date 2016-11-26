import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { HttpModule }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { CoreModule } from '../shared/core.module';
import { TilesModule  } from '../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';

import { MonitorRoutingModule } from './monitor.routes';
import { MonitorListComponent } from './monitor-list.component';

import {
    GrowlModule,
    DataTableModule,
    SharedModule,
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,        
        HttpModule,
        RouterModule,

        //primeng
        GrowlModule,
        DataTableModule,
        SharedModule,

        MonitorRoutingModule,

        //d3s        
        CoreModule,        
        TilesModule,
        SharedGridPagingInfoModule,
    ],
    declarations: [        
        MonitorListComponent,        
    ]    
})
export class MonitorModule { }