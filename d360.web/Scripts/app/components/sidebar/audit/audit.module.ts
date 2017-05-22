import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

import {
    DataTableModule,
    SharedModule,
} from 'primeng/primeng';

import { CoreModule } from '../../shared/core.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { TilesModule  } from '../../shared/tiles/tiles.module';

import { AuditRoutingModule } from './audit.routes';

import { AuditComponent } from './audit.component';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,

        //routing 
        AuditRoutingModule,

        //d3s        
        CoreModule,
        SharedGridPagingInfoModule,
        TilesModule,

        //prime        
        DataTableModule,
        SharedModule,
    ],
    declarations: [
        AuditComponent,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class AuditModule { }