import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

import {    
    ButtonModule,    
} from 'primeng/primeng';

import { CoreModule } from '../core.module';
import { TilesModule  } from '../tiles/tiles.module';

import { DashboardTabComponent } from './dashboard-tab.component';
import { PowerBIViewerComponent } from './powerbi-viewer.component';

@NgModule({
    imports: [CommonModule,  
        FormsModule,
        HttpModule,
        //d3s
        CoreModule,
        TilesModule,

        //prime
        ButtonModule,
    ],
    declarations: [
        DashboardTabComponent,
        PowerBIViewerComponent
    ],
    exports: [
        DashboardTabComponent
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class SharedDashboardModule { }