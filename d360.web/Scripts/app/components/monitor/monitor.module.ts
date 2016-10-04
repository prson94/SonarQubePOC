import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { CoreModule } from '../shared/core.module';
import { SharedModule } from '../shared/shared.module';
import { PipesModule } from '../../pipes/pipes.module';

import { MonitorComponent } from './monitor.component';
import { MonitorListComponent } from './monitor-list.component';

import {
    GrowlModule,
    DataTableModule,
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,

        //primeng
        GrowlModule,
        DataTableModule,


        //d3s
        SharedModule,
        CoreModule,
        PipesModule,
    ],
    declarations: [
        MonitorComponent,
        MonitorListComponent,        
    ],
    exports: [
        MonitorComponent,
        MonitorListComponent,        
    ]
})
export class MonitorModule { }