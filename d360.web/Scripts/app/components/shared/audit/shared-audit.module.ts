import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { HttpModule, XHRBackend  }     from '@angular/http';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

import {    
    DataTableModule,    
    SharedModule,
} from 'primeng/primeng';

import { CoreModule } from '../core.module';
import { SharedGridPagingInfoModule } from '../grid-paging-info.component';
import { TilesModule  } from '../tiles/tiles.module';

import { AuditComponent } from './audit.component';


@NgModule({
    imports: [CommonModule,        
        HttpModule,

        //d3s
        CoreModule,        
        SharedGridPagingInfoModule,    
        TilesModule,    

        //prime        
        DataTableModule,        
        SharedModule,
    ],
    declarations: [
        AuditComponent
    ],
    exports: [
        AuditComponent
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class SharedAuditModule { }