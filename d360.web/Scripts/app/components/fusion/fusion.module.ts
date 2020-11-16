import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../http-interceptors/govern-request.interceptor";
import { RouterModule } from '@angular/router';


import { CoreModule } from '../shared/core.module';
import { D3SSharedModule } from '../shared/shared.module';
import { PipesModule } from '../../pipes/pipes.module';
import { TilesModule  } from '../shared/tiles/tiles.module';
import { SharedDeleteFormModule } from '../shared/delete.form';
import { SharedDynamicGridEditorModule } from '../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedFusionAttributeItemDetailsModule } from '../shared/fusion-attribute-item-details.component';
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
import { FusionStatisticsComponent } from './fusion-statistics.component';
import { FusionStructureTreeComponent } from './fusion-structure-tree.component';
import { FusionAttributeSummaryFiltersComponent } from './fusion-attribute-summary-filters.component';
import { FusionQueryListComponent } from './fusion-query-list.component';
import { FusionQueryAttributeEditorComponent } from './fusion-query-attribute-editor.component';
import { FusionHistoryComponent } from './fusion-history.component'
import { FusionAttributeTabsComponent } from './fusion-attribute-tabs.component';
import { FusionAttributeComponent } from './fusion-attribute.component';

import { TabViewModule } from 'primeng/tabview';
import { ButtonModule } from 'primeng/button';
import { SharedModule } from 'primeng/api';
import { InputTextModule } from 'primeng/inputtext';
import { InputMaskModule } from 'primeng/inputmask';
import { DropdownModule } from 'primeng/dropdown';
import { TableModule } from 'primeng/table';
import { MultiSelectModule } from 'primeng/multiselect';
import { TooltipModule } from 'primeng/tooltip';
import { SelectButtonModule } from 'primeng/selectbutton';
import { TreeTableModule } from 'primeng/treetable';
import { ToastModule } from 'primeng/toast';
import { FileUploadModule } from 'primeng/fileupload';
import { TreeModule } from 'primeng/tree';

import { CodemirrorModule } from 'ng2-codemirror';

/*
declare var require: any;

export function highchartsFactory() {
    const highcharts = require('highcharts');
    const highChartsMore = require('highcharts/highcharts-more');
    const solidGauge = require('highcharts/modules/solid-gauge');
    ChartModule.forRoot(require('highcharts'),
        require('highcharts/highcharts-more'),
        require('highcharts/modules/solid-gauge'));
    return highcharts;
}*/


@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpClientModule,
        RouterModule,

        FusionRoutingModule,

        //primeng
        ToastModule,
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
                
        //d3s        
        CoreModule,
        D3SSharedModule,        
        PipesModule,      
          
        SharedDeleteFormModule,        
        SharedDynamicGridEditorModule,
        SharedFieldDefinitionModule,
        SharedFusionAttributeItemDetailsModule,
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
        FusionStatisticsComponent,
        FusionStructureTreeComponent,
        FusionQueryListComponent,
        FusionQueryAttributeEditorComponent,  
        FusionAttributeTabsComponent,
        FusionAttributeComponent
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true
        }        
    ]
})
export class FusionModule { }