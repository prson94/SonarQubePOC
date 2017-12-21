import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

import { CoreModule } from '../../shared/core.module';
import { TilesModule  } from '../../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../../shared/delete.form';
import { SharedDynamicGridEditorModule } from '../../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedObjectDetailsModule } from '../../shared/objectdetails/shared-object-details.module';

import { AdminAnalyticsComponent } from './admin-analytics.component';
import { AdminMetricGroupEditorComponent } from './admin-metric-group-editor.component';
import { AdminMetricGroupListComponent } from './admin-metric-group-list.component';
import { AdminMetricItemListComponent } from './admin-metric-item-list.component';
import { AdminMetricMapListComponent } from './admin-metric-map-list.component';
import { AdminMetricConditionListComponent } from './admin-metric-condition-list.component';


import { AdminAnalyticsRoutingModule } from './admin-analytics.routes';

import {
    ButtonModule,
    DropdownModule,
    EditorModule,
    InputTextModule,
    MultiSelectModule,
    SharedModule,
    DataTableModule,
    TreeTableModule,
    CalendarModule,
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
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
        TreeTableModule,
        CalendarModule,

        //d3s        
        CoreModule,
        SharedGridPagingInfoModule,
        SharedDeleteFormModule,        
        SharedObjectDetailsModule,
        SharedDynamicGridEditorModule,
        TilesModule,
    ],
    declarations: [
        AdminAnalyticsComponent,
        AdminMetricGroupEditorComponent,
        AdminMetricGroupListComponent,
        AdminMetricItemListComponent,
        AdminMetricMapListComponent,
        AdminMetricConditionListComponent,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class AdminAnalyticsModule { }