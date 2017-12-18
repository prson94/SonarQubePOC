import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

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
        HttpModule,
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
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class VisualizationModule { }