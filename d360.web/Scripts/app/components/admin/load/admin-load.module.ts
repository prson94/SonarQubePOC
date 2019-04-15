import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

import { CoreModule } from '../../shared/core.module';
import { PipesModule } from '../../../pipes/pipes.module';
import { TilesModule  } from '../../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../../shared/delete.form';
import { SharedObjectDetailsModule } from '../../shared/objectdetails/shared-object-details.module';

import { BulkLoadItemComponent } from './bulk/bulk-load-item.component';
import { AdminLoadFormComponent } from './form/admin-load-form.component';
import { AdminLoadComponent } from './admin-load.component';

import { AdminLoadRoutingModule } from './admin-load.routes';

import {
    ButtonModule,
    DropdownModule,
    InputTextModule,
    SharedModule,
} from 'primeng/primeng';

import { TableModule } from 'primeng/table';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpModule,

        AdminLoadRoutingModule,

        //prime
        ButtonModule,
        DropdownModule,
        InputTextModule,
        SharedModule,
        TableModule,

        //d3s        
        CoreModule,
        PipesModule,        
        SharedDeleteFormModule,                
        SharedGridPagingInfoModule,
        SharedObjectDetailsModule,
        TilesModule,
    ],
    declarations: [
        BulkLoadItemComponent,
        AdminLoadFormComponent,
        AdminLoadComponent,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})

export class AdminLoadModule { }
