import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { CoreModule } from '../shared/core.module';
import { D3SSharedModule } from '../shared/shared.module';
import { PipesModule } from '../../pipes/pipes.module';
import { TilesModule  } from '../shared/tiles/tiles.module';

import { MonitorRoutingModule } from './monitor.routes';
import { MonitorListComponent } from './monitor-list.component';

import {
    GrowlModule,
    DataTableModule,
    SharedModule,
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,

        //primeng
        GrowlModule,
        DataTableModule,
        SharedModule,

        MonitorRoutingModule,

        //d3s
        D3SSharedModule,
        CoreModule,
        PipesModule,
        TilesModule,
    ],
    declarations: [        
        MonitorListComponent,        
    ]    
})
export class MonitorModule { }