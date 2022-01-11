import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';



import { RouterModule } from '@angular/router';


import { ButtonModule } from 'primeng/button';

import { CoreModule } from '../../shared/core.module';
import { TilesModule } from '../../shared/tiles/tiles.module';

import { DashboardRoutingModule } from './dashboard.routes';

import { DashboardComponent } from './dashboard.component';
import { PowerBIViewerComponent } from './powerbi-viewer.component';
import { SagacityViewerComponent } from './sagacity-viewer.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,

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
        
    ]
})
export class DashboardModule { }