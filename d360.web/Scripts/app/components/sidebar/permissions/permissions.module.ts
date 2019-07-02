import { NgModule } from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernPostRequestInterceptor } from "../../../http-interceptors/govern-post-request.interceptor";

import { RouterModule } from '@angular/router';

import {
    ButtonModule,
} from 'primeng/primeng';

import { CoreModule } from '../../shared/core.module';
import { TilesModule } from '../../shared/tiles/tiles.module';

import { PermissionsRoutingModule } from './permissions.routes';

import { PermissionsComponent } from './permissions.component';
import { SharedFieldDefinitionModule } from '../../shared/fielddefinition/shared-field-definition.module';
import { AdminModule } from '../../admin/admin.module';
import { SharedResponsibilitiesModule } from '../../shared/responsibilities/shared-responsibilities.module';


@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,
        RouterModule,

        //routing 
        PermissionsRoutingModule,
        
        //prime
        ButtonModule,

        //d3s        
        CoreModule,
        TilesModule,
        SharedFieldDefinitionModule,
        SharedResponsibilitiesModule,
        AdminModule,
    ],
    declarations: [
        PermissionsComponent,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernPostRequestInterceptor,
            multi: true },
    ]
})
export class PermissionsModule { }