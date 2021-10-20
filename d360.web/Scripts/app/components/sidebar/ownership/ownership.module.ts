import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";

import { RouterModule } from '@angular/router';

import { SharedModule } from 'primeng/api';

import { CoreModule } from '../../shared/core.module';
import { SharedResponsibilitiesModule } from '../../shared/responsibilities/shared-responsibilities.module';
import { TilesModule } from '../../shared/tiles/tiles.module';

import { OwnershipRoutingModule } from './ownership.routes';

import { OwnershipComponent } from './ownership.component';
import { PeopleResponsibilitiesModule } from '../../shared/responsibilities/people-responsibilities.tile';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        HttpClientModule,
        RouterModule,

        //routing 
        OwnershipRoutingModule,

        //d3s        
        CoreModule,
        SharedResponsibilitiesModule,
        TilesModule,
        PeopleResponsibilitiesModule,

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