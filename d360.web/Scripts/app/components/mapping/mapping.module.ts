import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../authentication-connection-backend';

import { CoreModule } from '../shared/core.module';
import { TilesModule  } from '../shared/tiles/tiles.module';
import { SharedAuditModule } from '../shared/audit/shared-audit.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { SharedRelationshipModule } from '../shared/relationship/shared-relationship.module';
import { SharedDeleteFormModule } from '../shared/delete.form';

import { MappingRoutingModule } from './mapping.routes';

import { MappingComponent } from './mapping.component';

import {    
    DataTableModule,        
    SharedModule,
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,

        //routing 
        MappingRoutingModule,

        //d3s        
        CoreModule,
        SharedAuditModule,
        SharedDeleteFormModule,
        SharedGridPagingInfoModule,
        SharedRelationshipModule,
        TilesModule,

        //prime
        DataTableModule,
        SharedModule,
    ],
    declarations: [
        MappingComponent,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class MappingModule { }