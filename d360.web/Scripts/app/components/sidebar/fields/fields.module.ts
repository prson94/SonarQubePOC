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
import { FieldsRoutingModule } from './fields.routes';

import { FieldDefinitionComponent } from './field-definition.component';
import { SharedFieldDefinitionModule } from '../../shared/fielddefinition/shared-field-definition.module';


@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,
        RouterModule,

        //routing 
        FieldsRoutingModule,

        //prime
        ButtonModule,

        //d3s        
        CoreModule,
        TilesModule,
        SharedFieldDefinitionModule,
        
    ],
    declarations: [
        FieldDefinitionComponent,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernPostRequestInterceptor,
            multi: true },
    ]
})
export class FieldsModule { }