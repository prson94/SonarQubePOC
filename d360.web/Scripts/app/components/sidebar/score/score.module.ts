import { NgModule, Component } from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';


import {
    ButtonModule,
} from 'primeng/primeng';

import { CoreModule } from '../../shared/core.module';
import { TilesModule } from '../../shared/tiles/tiles.module';
import { ScoreRoutingModule } from './score.routes';
import { ScoreComponent } from './score.component';
import { ResourceModule } from '../../resource/resource.module';
import { SharedObjectGovernanceModule } from '../../shared/objectgovernance/shared-object-governance.module';
import { GovernRequestInterceptor } from '../../../http-interceptors/govern-request.interceptor';
import { HTTP_INTERCEPTORS } from '@angular/common/http';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,        
        RouterModule,

        //routing 
        ScoreRoutingModule,

        //prime
        ButtonModule,

        //d3s        
        CoreModule,
        TilesModule,
        ResourceModule,
        SharedObjectGovernanceModule,
    ],
    declarations: [
        ScoreComponent
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true
        },
    ]
})
export class ScoreModule { }