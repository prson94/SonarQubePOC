import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpModule, XHRBackend } from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

import {
    ButtonModule,
} from 'primeng/primeng';

import { CoreModule } from '../../shared/core.module';
import { TilesModule } from '../../shared/tiles/tiles.module';

import { ItemFollowRoutingModule } from './itemfollow.routes';

import { ItemFollowComponent } from './itemfollow.component';
import { ResourceModule } from '../../resource/resource.module';


@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
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
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class ItemFollowModule { }