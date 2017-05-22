import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';


import { AceEditorModule } from '../../shared/ace-editor.module';
import { CoreModule } from '../../shared/core.module';
import { TilesModule  } from '../../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../../shared/delete.form';

import { SharedObjectDetailsModule } from '../../shared/objectdetails/shared-object-details.module';

import { AdminDashboardsComponent } from './admin-dashboards.component';
import { AdminDashboardsEditor } from './admin-dashboards-editor.component';
import { AdminReportItemsComponent } from './admin-report-items.component';
import { AdminReportTileEditorComponent } from './admin-report-tile-editor.component';

import { AdminDashboardsRoutingModule } from './admin-dashboards.routes';

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

        AdminDashboardsRoutingModule,

        //prime
        ButtonModule,
        DropdownModule,
        EditorModule,
        InputTextModule,
        MultiSelectModule,
        SharedModule,
        DataTableModule,

        //d3s   
        AceEditorModule,
        CoreModule,
        SharedGridPagingInfoModule,
        SharedDeleteFormModule,
        
        SharedObjectDetailsModule,
        TilesModule,
    ],
    declarations: [
        AdminDashboardsComponent,
        AdminDashboardsEditor,
        AdminReportItemsComponent,
        AdminReportTileEditorComponent,
        
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class AdminDashboardsModule { }