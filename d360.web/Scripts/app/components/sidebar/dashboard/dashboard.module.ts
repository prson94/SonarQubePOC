import { NgModule } from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpModule, XHRBackend } from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

import {
    ButtonModule,
} from 'primeng/primeng';

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
        HttpModule,
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
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class DashboardModule { }