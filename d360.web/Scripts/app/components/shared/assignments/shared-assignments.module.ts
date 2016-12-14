import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { HttpModule, XHRBackend  }     from '@angular/http';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

import {
    DataTableModule,
    SharedModule,
} from 'primeng/primeng';

import { CoreModule } from '../core.module';
import { TilesModule  } from '../tiles/tiles.module';

import { AssignmentsComponent } from './assignments.component';


@NgModule({
    imports: [CommonModule,
        HttpModule,

        //d3s
        CoreModule,        
        TilesModule,

        //prime        
        DataTableModule,
        SharedModule,
    ],
    declarations: [
        AssignmentsComponent
    ],
    exports: [
        AssignmentsComponent
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class SharedAssignmentsModule { }