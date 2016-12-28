import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { RouterModule }    from '@angular/router';
import { HttpModule, XHRBackend  }     from '@angular/http';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

import {
    ButtonModule,
    DataTableModule,    
    SharedModule,    
} from 'primeng/primeng';

import { ChartModule } from 'angular2-highcharts';

import { CoreModule } from '../core.module';
import { TilesModule  } from '../tiles/tiles.module';
import { SocialModule } from '../social/social.module';
import { WorkflowModule } from '../../workflow/workflow.module';
import { SharedGridPagingInfoModule } from '../grid-paging-info.component';

import { ArtifactStatusComponent } from './artifact-status.component';
import { ObjectBoardComponent } from './object-board.component';
import { ObjectGovernanceComponent } from './object-governance.component';
import { ObjectHealthDetailsComponent } from './object-health-details.component';
import { ObjectHealthComponent } from './object-health.component';
import { ObjectIssuesComponent } from './object-issues.component';

@NgModule({
    imports: [CommonModule,
        RouterModule,
        HttpModule,
        //d3s
        CoreModule,
        SharedGridPagingInfoModule,        
        SocialModule,
        TilesModule,
        WorkflowModule,
        //prime        
        ButtonModule,
        DataTableModule,        
        SharedModule,  

        //charts
        ChartModule,
    ],
    declarations: [
        ArtifactStatusComponent,
        ObjectBoardComponent,
        ObjectGovernanceComponent,
        ObjectHealthDetailsComponent,
        ObjectHealthComponent,
        ObjectIssuesComponent,
    ],
    exports: [
        ObjectBoardComponent,        
        ObjectGovernanceComponent,
        ObjectHealthComponent,
        ObjectHealthDetailsComponent,     
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class SharedObjectGovernanceModule { }