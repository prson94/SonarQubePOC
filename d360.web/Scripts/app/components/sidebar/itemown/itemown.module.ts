import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';



import { RouterModule } from '@angular/router';

import { ButtonModule } from 'primeng/button';

import { CoreModule } from '../../shared/core.module';
import { TilesModule } from '../../shared/tiles/tiles.module';

import { ItemOwnRoutingModule } from './itemown.routes';

import { ItemOwnComponent } from './itemown.component';
import { D3SSharedModule } from '../../shared/shared.module';


@NgModule({
    imports: [
        CommonModule,
        FormsModule,

        RouterModule,

        //routing 
        ItemOwnRoutingModule,

        //prime
        ButtonModule,

        //d3s        
        CoreModule,
        TilesModule,
        D3SSharedModule
    ],
    declarations: [
        ItemOwnComponent
    ],
    providers: [

    ]
})
export class ItemOwnModule { }