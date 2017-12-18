import { NgModule } from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpModule, XHRBackend } from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

import {
    SharedModule,
} from 'primeng/primeng';

import { CoreModule } from '../../shared/core.module';
import { SharedResponsibilitiesModule } from '../../shared/responsibilities/shared-responsibilities.module';
import { TilesModule } from '../../shared/tiles/tiles.module';

import { OwnershipRoutingModule } from './ownership.routes';

import { OwnershipComponent } from './ownership.component';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpModule,
        RouterModule,

        //routing 
        OwnershipRoutingModule,

        //d3s        
        CoreModule,
        SharedResponsibilitiesModule,
        TilesModule,

        //prime        
        SharedModule,
    ],
    declarations: [
        OwnershipComponent,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class OwnershipModule { }