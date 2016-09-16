import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { SharedModule } from '../shared/shared.module';

import { FusionAgentHistoryComponent } from './fusion-agent-history.component';
import { FusionComponent } from './fusion.component';
import { FusionConfigurationComponent } from './fusion-configurations.component';
import { FusionExecutionHistoryComponent } from './fusion-execution-history.component';
import { FusionItemComponent } from './fusion-item.component';
import { FusionListComponent } from './fusion-list.component';
import { FusionPromotionHistoryComponent } from './fusion-promotion-history.component';
import { FusionStatisticsComponent } from './fusion-statistics.component';

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
        DragDropModule,
        PaginatorModule,

        //d3s
        SharedModule,

    ],
    declarations: [
        FusionAgentHistoryComponent,
        FusionComponent,
        FusionConfigurationComponent,
        FusionExecutionHistoryComponent,
        FusionItemComponent,
        FusionListComponent,
        FusionPromotionHistoryComponent,
        FusionStatisticsComponent,
    ],
    exports: [
        FusionAgentHistoryComponent,
        FusionComponent,
        FusionConfigurationComponent,
        FusionExecutionHistoryComponent,
        FusionItemComponent,
        FusionListComponent,
        FusionPromotionHistoryComponent,
        FusionStatisticsComponent,
    ]
})
export class FusionModule { }