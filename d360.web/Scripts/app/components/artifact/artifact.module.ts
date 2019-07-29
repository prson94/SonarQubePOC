import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { RouterModule } from '@angular/router';

import { ChartModule } from 'angular2-highcharts';

import { CoreModule } from '../shared/core.module';
import { WorkflowModule } from '../workflow/workflow.module';
import { D3SSharedModule } from '../shared/shared.module';
import { PipesModule } from '../../pipes/pipes.module';
import { TilesModule  } from '../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../shared/delete.form';
import { SharedDynamicGridEditorModule } from '../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';

import { ArtifactRoutingModule } from './artifact.routes';

import { ArtifactColumnFilterComponent } from './artifact-column-filter.component';
import { ArtifactComponent } from './artifact.component';
import { ArtifactDefnintionComponent } from './artifact-definition.component';
import { ArtifactGridComponent } from './artifact-grid.component';
import { ArtifactItemComponent } from './artifact-item.component';
import { ArtifactListComponent } from './artifact-list.component';
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

import {    
    InputTextModule,    
    CalendarModule,
    TreeTableModule,
    ButtonModule,
    DropdownModule,    
    SelectButtonModule,    
    MultiSelectModule,    
    TooltipModule,        
    SharedModule,
} from 'primeng/primeng';

import { TableModule } from 'primeng/table';
import {GovernRequestInterceptor} from "../../http-interceptors/govern-request.interceptor";
import { SharedObjectGovernanceModule } from '../shared/objectgovernance/shared-object-governance.module';


@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,
        RouterModule,

        ArtifactRoutingModule,

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
        TilesModule,
        WorkflowModule,        
    ],
    declarations: [        
        ArtifactColumnFilterComponent,
        ArtifactComponent,
        ArtifactCustomExportComponent,
        ArtifactDefnintionComponent,
        ArtifactGridComponent,
        ArtifactItemComponent,
        ArtifactListComponent,
        ArtifactTopLevelListComponent,        
        ArtifactTopLevelFilterComponent,
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

export class ArtifactModule { }
