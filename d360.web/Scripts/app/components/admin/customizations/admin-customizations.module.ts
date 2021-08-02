import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";

import { CoreModule } from '../../shared/core.module';
import { PipesModule } from '../../../pipes/pipes.module';
import { TilesModule } from '../../shared/tiles/tiles.module';
import { AdminModule } from '../admin.module';

import { AdminCustomizationsComponent } from './admin-customizations.component';
import { AdminCustomizationsRoutingModule } from './admin-customizations.routes';
import { IgMessageBoxModule } from '../../shared/controls/message-box/message-box.module';

import { CodemirrorModule } from 'ng2-codemirror';

import { ButtonModule } from 'primeng/button';
import { SharedModule } from 'primeng/api';

@NgModule({
    imports: [
        CommonModule,
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

        IgMessageBoxModule,
    ],
    declarations: [
        AdminCustomizationsComponent
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
    ]
})
export class AdminCustomizationsModule { }