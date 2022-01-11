import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';

import { CoreModule } from '../../shared/core.module';
import { PipesModule } from '../../../pipes/pipes.module';
import { TilesModule } from '../../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../../shared/delete.form';
import { SharedDynamicGridEditorModule } from '../../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedObjectDetailsModule } from '../../shared/objectdetails/shared-object-details.module';

import { ScoringIndexComponent } from './index.component';
import { ScoringDetailComponent } from './detail.component';
import { AdminScoringRoutingModule } from './admin-scoring.routes';
import { DataQualityMeasureEditorComponent } from './measure-editor-dataquality.component';
import { ExternalMeasureEditorComponent } from './measure-editor-external.component';
import { GovernanceMeasureEditorComponent } from './measure-editor-governance.component';
import { MeasureListComponent } from './measure-list.component';

import { SharedModule } from 'primeng/api';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { DropdownModule } from 'primeng/dropdown';
import { CalendarModule } from 'primeng/calendar';
import { SpinnerModule } from 'primeng/spinner';
import { CheckboxModule } from 'primeng/checkbox';
import { MultiSelectModule } from 'primeng/multiselect';
import { RadioButtonModule } from 'primeng/radiobutton';
import { TreeTableModule } from 'primeng/treetable';
import { TooltipModule } from 'primeng/tooltip';
import { ToggleButtonModule } from 'primeng/togglebutton';
import { InputTextareaModule } from 'primeng/inputtextarea';
import { EditorModule } from 'primeng/editor';
import { TableModule } from 'primeng/table';
import { SliderModule } from 'primeng/slider';

import { SimpleAccordionModule } from '../../shared/simple-accordion.part';
import { DirectivesModule } from '../../../directives/directives.module';

import { SiteModalModule } from '../../shared/modal/gov-modal.module';
import { AllocationEditorComponent } from './allocation-editor.component';
import { AutoCompleteModule } from 'primeng/autocomplete';

import { InfoTooltipModule } from '../../shared/tooltip/info-tooltip.component';
import { MenuModule } from 'primeng/menu';
import { SwitchModule } from '../../shared/controls/switch/switch';
import { AdminMeasureHistoryComponent } from './measure-history.component'
import { IgDateModule } from '../../shared/controls/date/date';
import { PropertyGroupModule } from '../../shared/controls/property-group/property-group.component';
import { IgNumberFieldModule } from '../../shared/controls/number-picker/number-input.component';
import { FieldConditionGridModule } from '../../shared/controls/field-condition-grid/field-condition-grid.module';
import { IgMessageBoxModule } from '../../shared/controls/message-box/message-box.module';
import { PopupMenuModule } from '../../shared/controls/popup-menu/popup-menu.component';
import { IgBadgeModule } from '../../shared/controls/badge/badge.module';
import { MetricPassTestDetailsModule } from './admin-metric-pass-test-details.module';
import { ScoreDefinitionModule } from '../../shared/asset-score/definition/score-definition.module';


@NgModule({
    imports: [
        CommonModule,
        FormsModule,

        ReactiveFormsModule,

        AdminScoringRoutingModule,

        //prime
        ButtonModule,
        CalendarModule,
        DropdownModule,
        EditorModule,
        CheckboxModule,
        InputTextModule,
        ToggleButtonModule,
        InputTextareaModule,
        MultiSelectModule,
        RadioButtonModule,
        SharedModule,
        SpinnerModule,
        TreeTableModule,
        TableModule,
        AutoCompleteModule,
        SliderModule,
        MenuModule,
        TooltipModule,
        //d3s        
        CoreModule,
        PipesModule,
        SharedGridPagingInfoModule,
        SharedDeleteFormModule,
        SharedObjectDetailsModule,
        SharedDynamicGridEditorModule,
        TilesModule,
        SimpleAccordionModule,
        TooltipModule,
        DirectivesModule,
        SiteModalModule,
        InfoTooltipModule,
        SwitchModule,
        IgDateModule,
        PropertyGroupModule,
        IgNumberFieldModule,
        FieldConditionGridModule,
        IgMessageBoxModule,
        PopupMenuModule,
        IgBadgeModule,
        ScoreDefinitionModule,

        MetricPassTestDetailsModule

    ],
    declarations: [
        ScoringIndexComponent,
        ScoringDetailComponent,

        AllocationEditorComponent,
        DataQualityMeasureEditorComponent,
        ExternalMeasureEditorComponent,
        GovernanceMeasureEditorComponent,

        MeasureListComponent,
        AdminMeasureHistoryComponent
    ],
    providers: [
    ]
})

export class AdminScoringModule {
}
