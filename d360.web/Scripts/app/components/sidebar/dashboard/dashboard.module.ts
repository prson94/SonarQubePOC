import { NgModule } from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";

import { RouterModule } from '@angular/router';


import { ButtonModule } from 'primeng/button';

import { CoreModule } from '../../shared/core.module';
import { TilesModule } from '../../shared/tiles/tiles.module';

import { DashboardRoutingModule } from './dashboard.routes';

import { DashboardComponent } from './dashboard.component';
import { PowerBIViewerComponent } from './powerbi-viewer.component';
import { SagacityViewerComponent } from './sagacity-viewer.component';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,
        RouterModule,

        //routing 
        DashboardRoutingModule,

        //prime
        ButtonModule,

        //d3s        
        CoreModule,        
        TilesModule,        

    ],
    declarations: [
        DashboardComponent,
        PowerBIViewerComponent,
        SagacityViewerComponent,
    ],
    exports: [
        PowerBIViewerComponent,
        SagacityViewerComponent,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true},
    ]
})
export class DashboardModule { }