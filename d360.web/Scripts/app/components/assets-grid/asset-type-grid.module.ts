import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { RouterModule } from '@angular/router';

import { ChartModule } from 'angular2-highcharts';

import { CoreModule } from '../shared/core.module';
import { WorkflowModule } from '../workflow/workflow.module';
import { D3SSharedModule } from '../shared/shared.module';
import { PipesModule } from '../../pipes/pipes.module';
import { TilesModule } from '../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../shared/delete.form';
import { SharedDynamicGridEditorModule } from '../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedAssetEditorsModule } from '../shared/asseteditors/shared-asset-editor.module';

import { ArtifactColumnFilterComponent } from './artifact-column-filter.component';
import { ArtifactDefnintionComponent } from './artifact-definition.component';
import { ArtifactGridComponent } from './artifact-grid.component';
import { ArtifactTopLevelListComponent } from './artifact-top-level-list.component';
import { ArtifactTopLevelFilterComponent } from './artifact-top-level-filter.component';
import { ArtifactCustomExportComponent } from './artifact-custom-export.component';
import { HighchartsStatic } from 'angular2-highcharts/dist/HighchartsService';


declare var require: any;
export function highchartsFactory() {
    const hc = require('highcharts');
    const hcm = require('highcharts/highcharts-more'); // used for more category of charts    
    const solidGauge = require('highcharts/modules/solid-gauge');
    hcm(hc);
    solidGauge(hc);
    return hc;
}


import { SharedModule } from 'primeng/shared';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TreeTableModule } from 'primeng/treetable';
import { CalendarModule } from 'primeng/calendar';
import { SelectButtonModule } from 'primeng/selectbutton';
import { DropdownModule } from 'primeng/dropdown';
import { TableModule } from 'primeng/table';
import { MultiSelectModule } from 'primeng/multiselect';
import { TooltipModule } from 'primeng/tooltip';
import { GovernRequestInterceptor } from "../../http-interceptors/govern-request.interceptor";
import { SharedObjectGovernanceModule } from '../shared/objectgovernance/shared-object-governance.module';


@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        HttpClientModule,
        RouterModule,

        //primeng        
        InputTextModule,
        CalendarModule,
        TreeTableModule,
        ButtonModule,
        DropdownModule,
        SelectButtonModule,
        MultiSelectModule,
        TooltipModule,
        SharedModule,
        TableModule,

        //highcharts        
        ChartModule,

        //d3s
        D3SSharedModule,
        CoreModule,
        PipesModule,

        SharedDeleteFormModule,
        SharedGridPagingInfoModule,
        SharedDynamicGridEditorModule,
        SharedObjectGovernanceModule,
        SharedAssetEditorsModule,
        TilesModule,
        WorkflowModule,
    ],
    declarations: [
        ArtifactColumnFilterComponent,
        ArtifactCustomExportComponent,
        ArtifactDefnintionComponent,
        ArtifactGridComponent,
        ArtifactTopLevelListComponent,
        ArtifactTopLevelFilterComponent,
    ],
    exports: [
        ArtifactColumnFilterComponent,
        ArtifactCustomExportComponent,
        ArtifactDefnintionComponent,
        ArtifactGridComponent,
        ArtifactTopLevelListComponent,
        ArtifactTopLevelFilterComponent
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true
        },
        {
            provide: HighchartsStatic,
            useFactory: highchartsFactory
        },
    ]
})

export class AssetTypeGridModule { }
