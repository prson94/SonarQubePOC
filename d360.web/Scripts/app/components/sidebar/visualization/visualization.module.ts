import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from '../../../http-interceptors/govern-request.interceptor';
import { RouterModule } from '@angular/router';

import { SharedModule } from 'primeng/api';

import { CoreModule } from '../../shared/core.module';
import { SharedDiagramModule } from '../../shared/diagram/shared-diagram.module';
import { TilesModule } from '../../shared/tiles/tiles.module';

import { VisualizationRoutingModule } from './visualization.routes';

import { BrowserComponent } from './browser.component';
import { DiagramComponent } from './diagram.component';
import { DeactivateGuard } from '../../../guards/deactivate.guard';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        HttpClientModule,
        RouterModule,

        //routing 
        VisualizationRoutingModule,

        //d3s        
        CoreModule,
        SharedDiagramModule,
        TilesModule,

        //prime                
        SharedModule,
    ],
    declarations: [
        BrowserComponent,
        DiagramComponent
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true
        },
        DeactivateGuard
    ]
})
export class VisualizationModule { }