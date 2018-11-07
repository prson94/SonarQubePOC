import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

import { CoreModule } from '../../shared/core.module';
import { TilesModule  } from '../../shared/tiles/tiles.module';
import { SharedFieldDefinitionModule } from '../../shared/fielddefinition/shared-field-definition.module';
import { D3SSharedModule } from '../../shared/shared.module';

import { AdminResourcesComponent } from './admin-resources.component';


import { AdminResourcesRoutingModule } from './admin-resources.routes';

import {
    ButtonModule,
    InputTextModule,
    SharedModule,
    GrowlModule
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpModule,

        AdminResourcesRoutingModule,

        //prime        
        SharedModule,

        //d3s                
        CoreModule, 
        D3SSharedModule,       
        SharedFieldDefinitionModule,        
        TilesModule,
    ],
    declarations: [
        AdminResourcesComponent,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class AdminResourcesModule { }