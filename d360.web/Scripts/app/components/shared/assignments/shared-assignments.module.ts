import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { HttpModule, XHRBackend  }     from '@angular/http';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

import {
    DataTableModule,
    SharedModule,
} from 'primeng/primeng';

import { TableModule } from 'primeng/table';

import { CoreModule } from '../core.module';
import { TilesModule  } from '../tiles/tiles.module';

import { AssignmentsComponent } from './assignments.component';
import { SharedGridPagingInfoModule } from '../grid-paging-info.component';


@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        HttpModule,

        //d3s
        CoreModule,        
        TilesModule,
        SharedGridPagingInfoModule,

        //prime        
        DataTableModule,
        SharedModule,
        TableModule,
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