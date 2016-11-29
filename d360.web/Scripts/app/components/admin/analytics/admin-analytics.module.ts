import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

import { CoreModule } from '../../shared/core.module';
import { TilesModule  } from '../../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../../shared/delete.form';
import { SharedAuditModule } from '../../shared/audit/shared-audit.module';
import { SharedObjectDetailsModule } from '../../shared/objectdetails/shared-object-details.module';

import { AdminStatisticsComponent } from './admin-statistics.component';
import { AdminStatisticCheckTypeInput } from './admin-statistic-checktype-input';
import { AdminStatisticEditor } from './admin-statistics-editor.component';

import { AdminAnalyticsRoutingModule } from './admin-analytics.routes';

import {
    ButtonModule,
    DropdownModule,
    EditorModule,
    InputTextModule,
    MultiSelectModule,
    SharedModule,
    DataTableModule,
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,

        AdminAnalyticsRoutingModule,

        //prime
        ButtonModule,
        DropdownModule,
        EditorModule,
        InputTextModule,
        MultiSelectModule,
        SharedModule,
        DataTableModule,

        //d3s        
        CoreModule,
        SharedGridPagingInfoModule,
        SharedDeleteFormModule,
        SharedAuditModule,
        SharedObjectDetailsModule,
        TilesModule,
    ],
    declarations: [
        AdminStatisticsComponent,
        AdminStatisticCheckTypeInput,
        AdminStatisticEditor,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class AdminAnalyticsModule { }