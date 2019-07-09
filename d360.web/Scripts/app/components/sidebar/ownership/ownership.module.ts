import { NgModule } from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";

import { RouterModule } from '@angular/router';

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
        HttpClientModule,
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
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
    ]
})
export class OwnershipModule { }