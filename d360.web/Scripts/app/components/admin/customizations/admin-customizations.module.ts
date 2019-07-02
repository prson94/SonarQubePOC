import { NgModule } from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernPostRequestInterceptor } from "../../../http-interceptors/govern-post-request.interceptor";

import { RouterModule } from '@angular/router';

import { CoreModule } from '../../shared/core.module';
import { PipesModule } from '../../../pipes/pipes.module';
import { TilesModule } from '../../shared/tiles/tiles.module';
import { AdminModule } from '../admin.module';

import { AdminCustomizationsComponent } from './admin-customizations.component';

import { AdminCustomizationsRoutingModule } from './admin-customizations.routes';

import { CodemirrorModule } from 'ng2-codemirror';

import {
    ButtonModule,    
    SharedModule,    
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,

        AdminCustomizationsRoutingModule,

        //code editor
        CodemirrorModule,

        //prime
        ButtonModule,
        SharedModule,

        //d3s        
        CoreModule,
        PipesModule,        
        TilesModule,
        AdminModule,
    ],
    declarations: [
        AdminCustomizationsComponent
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernPostRequestInterceptor,
            multi: true },
    ]
})
export class AdminCustomizationsModule { }