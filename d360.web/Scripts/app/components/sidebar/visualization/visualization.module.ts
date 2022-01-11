import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

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
        
        DeactivateGuard
    ]
})
export class VisualizationModule { }