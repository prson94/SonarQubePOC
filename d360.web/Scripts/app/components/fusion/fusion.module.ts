import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule }     from '@angular/http';
import { RouterModule } from '@angular/router';


import { SharedModule } from '../shared/shared.module';
import { ChartModule } from  '../shared/chart.module';
import { PipesModule } from '../../pipes/pipes.module';

import { FusionAgentHistoryComponent } from './fusion-agent-history.component';
import { FusionAgentErrorsComponent } from './fusion-agent-errors.component';
import { FusionAttributeSummaryComponent } from './fusion-attribute-summary.component';
import { FusionComponent } from './fusion.component';
import { FusionConfigurationComponent } from './fusion-configurations.component';
import { FusionExecutionErrorsComponent } from './fusion-execution-errors.component';
import { FusionExecutionHistoryComponent } from './fusion-execution-history.component';
import { FusionExecutionResultsComponent } from './fusion-execution-results.component';
import { FusionItemComponent } from './fusion-item.component';
import { FusionListComponent } from './fusion-list.component';
import { FusionProcessErrorsComponent } from './fusion-process-errors.component';
import { FusionPromotionHistoryComponent } from './fusion-promotion-history.component';
import { FusionStatisticsComponent } from './fusion-statistics.component';
import { FusionStructureTreeComponent } from './fusion-structure-tree.component';
import { FusionAttributeSummaryFiltersComponent } from './fusion-attribute-summary-filters.component';

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
    AutoCompleteModule,
    MultiSelectModule,
    SpinnerModule,
    EditorModule,
    TooltipModule,
    DragDropModule,
    PaginatorModule,
    TreeModule,
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,

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
        AutoCompleteModule,
        MultiSelectModule,
        SpinnerModule,
        EditorModule,
        TooltipModule,
        TreeModule,
        DragDropModule,
        PaginatorModule,

        //d3s
        SharedModule,
        ChartModule,
        PipesModule,
    ],
    declarations: [
        FusionAgentErrorsComponent,
        FusionAgentHistoryComponent,        
        FusionAttributeSummaryComponent,
        FusionAttributeSummaryFiltersComponent,
        FusionComponent,
        FusionConfigurationComponent,
        FusionExecutionErrorsComponent,
        FusionExecutionHistoryComponent,
        FusionExecutionResultsComponent,
        FusionItemComponent,
        FusionListComponent,
        FusionProcessErrorsComponent,
        FusionPromotionHistoryComponent,
        FusionStatisticsComponent,
        FusionStructureTreeComponent,
    ],
    exports: [
        FusionAgentErrorsComponent,
        FusionAgentHistoryComponent,        
        FusionAttributeSummaryComponent,
        FusionAttributeSummaryFiltersComponent,
        FusionComponent,
        FusionConfigurationComponent,
        FusionExecutionErrorsComponent,
        FusionExecutionHistoryComponent,
        FusionExecutionResultsComponent,
        FusionItemComponent,
        FusionListComponent,
        FusionProcessErrorsComponent,
        FusionPromotionHistoryComponent,
        FusionStatisticsComponent,
        FusionStructureTreeComponent,
    ]
})
export class FusionModule { }