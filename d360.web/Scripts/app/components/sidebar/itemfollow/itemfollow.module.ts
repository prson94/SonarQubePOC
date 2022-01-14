import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';



import { RouterModule } from '@angular/router';

import { ButtonModule } from 'primeng/button';

import { CoreModule } from '../../shared/core.module';
import { TilesModule } from '../../shared/tiles/tiles.module';

import { ItemFollowRoutingModule } from './itemfollow.routes';

import { ItemFollowComponent } from './itemfollow.component';
import { ResourceModule } from '../../resource/resource.module';


@NgModule({
    imports: [
        CommonModule,
        FormsModule,

        RouterModule,

        //routing 
        ItemFollowRoutingModule,

        //prime
        ButtonModule,

        //d3s        
        CoreModule,
        TilesModule,
        ResourceModule

    ],
    declarations: [
        ItemFollowComponent
    ],
    providers: [

    ]
})
export class ItemFollowModule { }