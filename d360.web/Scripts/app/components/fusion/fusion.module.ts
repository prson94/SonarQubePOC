import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
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
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { SharedResponsibilitiesModule } from '../shared/responsibilities/shared-responsibilities.module';

import { FusionRoutingModule } from './fusion.routes';

import { FusionAgentHistoryComponent } from './fusion-agent-history.component';
import { FusionAgentErrorsComponent } from './fusion-agent-errors.component';
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
import { FusionRulesComponent } from './fusion-rules.component';
import { FusionRuleStepComponent } from './fusion-rule-step.component';
import { FusionRuleStepFindComponent } from './fusion-rule-step-find.component';
import { FusionRuleStepFindViaRelationComponent } from './fusion-rule-step-findviarelation.component';
import { FusionRuleStepLineageComponent } from './fusion-rule-step-lineage.component';
import { FusionRuleStepPromoteComponent } from './fusion-rule-step-promote.component';
import { FusionRuleStepRelateComponent } from './fusion-rule-step-relate.component';

import {
    GrowlModule,
    InputTextModule,
    InputMaskModule,
    DataTableModule,
    TreeTableModule,
    ButtonModule,
    DropdownModule,
    CheckboxModule,
    CalendarModule,
    MenuModule,
    MenubarModule,
    AccordionModule,
    SelectButtonModule,    
    MultiSelectModule,    
    TooltipModule,        
    TreeModule,
    FileUploadModule,
    SharedModule,
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,

        FusionRoutingModule,

        //primeng
        GrowlModule,
        InputTextModule,
        InputMaskModule,
        DataTableModule,
        TreeTableModule,
        ButtonModule,
        DropdownModule,
        CheckboxModule,
        CalendarModule,
        MenuModule,
        MenubarModule,
        AccordionModule,
        SelectButtonModule,        
        MultiSelectModule,        
        TooltipModule,
        TreeModule,                
        FileUploadModule,
        SharedModule,

        //highcharts
        ChartModule,

        //d3s
        CoreModule,
        D3SSharedModule,        
        PipesModule,        
        SharedDeleteFormModule,
        SharedGridPagingInfoModule,
        SharedResponsibilitiesModule,
        TilesModule,
    ],
    declarations: [
        FusionAgentErrorsComponent,
        FusionAgentHistoryComponent,  
        FusionAttributeItemComponent,      
        FusionAttributeSummaryComponent,
        FusionAttributeSummaryFiltersComponent,
        FusionComponent,
        FusionConfigurationComponent,
        FusionExecutionErrorsComponent,
        FusionExecutionHistoryComponent,
        FusionExecutionResultsComponent,
        FusionItemComponent,
        FusionListComponent,
        FusionManualLoadComponent,
        FusionProcessErrorsComponent,
        FusionPromotionHistoryComponent,
        FusionStatisticsComponent,
        FusionTechnicalMappingsComponent,
        FusionStructureTreeComponent,
        FusionRulesComponent,
        FusionRuleStepComponent,
        FusionRuleStepFindComponent,
        FusionRuleStepFindViaRelationComponent,
        FusionRuleStepLineageComponent,
        FusionRuleStepPromoteComponent,
        FusionRuleStepRelateComponent,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class FusionModule { }