import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernPostRequestInterceptor } from "../../../http-interceptors/govern-post-request.interceptor";

import { RouterModule } from '@angular/router';

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

import { CodemirrorModule } from 'ng2-codemirror';

import {
    ButtonModule,
    DropdownModule,
    EditorModule,
    InputTextModule,
    MultiSelectModule,
    SharedModule,
} from 'primeng/primeng';

import { TableModule } from 'primeng/table';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,

        AdminDashboardsRoutingModule,

        //prime
        ButtonModule,
        DropdownModule,
        EditorModule,
        InputTextModule,
        MultiSelectModule,
        SharedModule,
        TableModule,

        //editor
        CodemirrorModule,

        //d3s           
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
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernPostRequestInterceptor,
            multi: true },
    ]
})
export class AdminDashboardsModule { }