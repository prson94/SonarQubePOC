import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';


import { RouterModule } from '@angular/router';

import { SharedModule } from 'primeng/api';

import { CoreModule } from '../../shared/core.module';
import { SharedResponsibilitiesModule } from '../../shared/responsibilities/shared-responsibilities.module';
import { TilesModule } from '../../shared/tiles/tiles.module';

import { OwnershipRoutingModule } from './ownership.routes';

import { OwnershipComponent } from './ownership.component';
import { PeopleResponsibilitiesModule } from '../../shared/responsibilities/people-responsibilities.tile';
import { SecurityModule } from '../../shared/security/security.module';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,

        RouterModule,

        //routing 
        OwnershipRoutingModule,

        //d3s        
        CoreModule,
        SharedResponsibilitiesModule,
        TilesModule,
		PeopleResponsibilitiesModule,
		SecurityModule,

        //prime        
        SharedModule,
    ],
    declarations: [
        OwnershipComponent,
    ],
    providers: [

    ]
})
export class OwnershipModule { }