import { NgModule } from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule } from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../authentication-connection-backend';


import { ChartModule } from 'angular2-highcharts';

import { CoreModule } from '../shared/core.module';
import { D3SSharedModule } from '../shared/shared.module';
import { PipesModule } from '../../pipes/pipes.module';
import { TilesModule  } from '../shared/tiles/tiles.module';
import { SharedDeleteFormModule } from '../shared/delete.form';
import { SharedDynamicGridEditorModule } from '../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedFusionAttributeItemDetailsModule } from '../shared/fusion-attribute-item-details.component';
import { SharedFusionAttributeProfileDetailsModule } from '../shared/fusion-attribute-profile-details.component';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { SharedRelationshipModule } from '../shared/relationship/shared-relationship.module';
import { SharedFieldDefinitionModule } from '../shared/fielddefinition/shared-field-definition.module';


import { FusionRoutingModule } from './fusion.routes';

import { FusionAgentHistoryComponent } from './fusion-agent-history.component';
import { FusionAgentErrorsComponent } from './fusion-agent-errors.component';
import { FusionAttributeDetailsComponent } from './fusion-attribute-details.component';
import { FusionAttributeItemComponent } from './fusion-attribute-item.component';
import { FusionAttributeSummaryComponent } from './fusion-attribute-summary.component';
import { FusionComponent } from './fusion.component';
import { FusionConfigurationComponent } from './fusion-configurations.component';
import { FusionExecutionErrorsComponent } from './fusion-execution-errors.component';
import { FusionExecutionHistoryComponent } from './fusion-execution-history.component';
import { FusionExecutionResultsComponent } from './fusion-execution-results.component';
import { FusionItemComponent } from './fusion-item.component';
import { FusionListComponent } from './fusion-list.component';
import { FusionManualLoadComponent } from './fusion-manual-load.component';
import { FusionProcessErrorsComponent } from './fusion-process-errors.component';
import { FusionPromotionHistoryComponent } from './fusion-promotion-history.component';
import { FusionStatisticsComponent } from './fusion-statistics.component';
import { FusionTechnicalMappingsComponent } from './fusion-technical-mappings.component';
import { FusionStructureTreeComponent } from './fusion-structure-tree.component';
import { FusionAttributeSummaryFiltersComponent } from './fusion-attribute-summary-filters.component';
import { FusionQueryListComponent } from './fusion-query-list.component';
import { FusionQueryAttributeEditorComponent } from './fusion-query-attribute-editor.component';
import { FusionHistoryComponent } from './fusion-history.component'
import { FusionAttributeTabsComponent } from './fusion-attribute-tabs.component';
import { FustionAttributeComponent } from './fusion-attribute.component';

import {
    GrowlModule,
    InputTextModule,
    InputMaskModule,
    TreeTableModule,
    ButtonModule,
    DropdownModule,        
    SelectButtonModule,    
    MultiSelectModule,    
    TooltipModule,        
    TreeModule,
    FileUploadModule,
    SharedModule,
    TabViewModule,
} from 'primeng/primeng';

import { TableModule } from 'primeng/table';

import { CodemirrorModule } from 'ng2-codemirror';
import { HighchartsStatic } from 'angular2-highcharts/dist/HighchartsService';



declare var require: any;

export function highchartsFactory() {
    const highcharts = require('highcharts');
    const highChartsMore = require('highcharts/highcharts-more');
    const solidGauge = require('highcharts/modules/solid-gauge');
    ChartModule.forRoot(require('highcharts'),
        require('highcharts/highcharts-more'),
        require('highcharts/modules/solid-gauge'));
    return highcharts;
}


@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpModule,
        RouterModule,

        FusionRoutingModule,

        //primeng
        GrowlModule,
        InputTextModule,
        InputMaskModule,
        TreeTableModule,
        ButtonModule,
        DropdownModule,             
        SelectButtonModule,        
        MultiSelectModule,        
        TooltipModule,
        TreeModule,                
        FileUploadModule,
        SharedModule,
        TableModule,
        TabViewModule,

        //editor
        CodemirrorModule,

        //highcharts        
        ChartModule,

        //d3s        
        CoreModule,
        D3SSharedModule,        
        PipesModule,      
          
        SharedDeleteFormModule,        
        SharedDynamicGridEditorModule,
        SharedFieldDefinitionModule,
        SharedFusionAttributeItemDetailsModule,
        SharedFusionAttributeProfileDetailsModule,
        SharedGridPagingInfoModule,
        SharedRelationshipModule,
        TilesModule,        
    ],
    declarations: [
        FusionAgentErrorsComponent,
        FusionAgentHistoryComponent,  
        FusionAttributeDetailsComponent,
        FusionAttributeItemComponent,      
        FusionAttributeSummaryComponent,
        FusionAttributeSummaryFiltersComponent,
        FusionComponent,
        FusionConfigurationComponent,
        FusionExecutionErrorsComponent,
        FusionExecutionHistoryComponent,
        FusionExecutionResultsComponent,
        FusionHistoryComponent,
        FusionItemComponent,
        FusionListComponent,
        FusionManualLoadComponent,
        FusionProcessErrorsComponent,
        FusionPromotionHistoryComponent,
        FusionStatisticsComponent,
        FusionTechnicalMappingsComponent,
        FusionStructureTreeComponent,
        FusionQueryListComponent,
        FusionQueryAttributeEditorComponent,  
        FusionAttributeTabsComponent,
        FustionAttributeComponent,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
        {
            provide: HighchartsStatic,
            useFactory: highchartsFactory
        },
    ]
})
export class FusionModule { }