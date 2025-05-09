import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';


import { RouterModule } from '@angular/router';

import { SharedModule } from 'primeng/api';

import { CoreModule } from '../../shared/core.module';
import { TilesModule } from '../../shared/tiles/tiles.module';

import { OwnershipRoutingModule } from './ownership.routes';

import { OwnershipComponent } from './ownership.component';
import { OwnerList } from '../../../_shared/components/security/owner-list';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,

        RouterModule,

        //routing 
        OwnershipRoutingModule,

        //d3s        
        CoreModule,
        TilesModule,
		OwnerList,

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