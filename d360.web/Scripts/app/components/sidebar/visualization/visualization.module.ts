import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernPostRequestInterceptor } from "../../../http-interceptors/govern-post-request.interceptor";
import { RouterModule } from '@angular/router';

import {    
    SharedModule,
} from 'primeng/primeng';

import { CoreModule } from '../../shared/core.module';
import { SharedDiagramModule } from '../../shared/diagram/shared-diagram.module';
import { TilesModule  } from '../../shared/tiles/tiles.module';

import { VisualizationRoutingModule } from './visualization.routes';

import { LineageComponent } from './lineage.component';
import { ImpactComponent } from './impact.component';
import { DiagramComponent } from './diagram.component';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
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
        LineageComponent,
        ImpactComponent,
        DiagramComponent
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernPostRequestInterceptor,
            multi: true },
    ]
})
export class VisualizationModule { }