import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';



import { RouterModule } from '@angular/router';


import { ButtonModule } from 'primeng/button';

import { CoreModule } from '../../shared/core.module';
import { TilesModule } from '../../shared/tiles/tiles.module';

import { MemberGroupRoutingModule } from './membergroup.routes';

import { MemberGroupComponent } from './membergroup.component';
import { ResourceModule } from '../../resource/resource.module';


@NgModule({
    imports: [
        CommonModule,
        FormsModule,

        RouterModule,

        //routing 
        MemberGroupRoutingModule,

        //prime
        ButtonModule,

        //d3s        
        CoreModule,
        TilesModule,
        ResourceModule

    ],
    declarations: [
        MemberGroupComponent,
    ],
    providers: [
        
    ]
})
export class MemberGroupModule { }