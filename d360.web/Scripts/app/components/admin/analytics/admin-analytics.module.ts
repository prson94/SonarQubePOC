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

import { AdminAnalyticsComponent } from './admin-analytics.component';
import { AdminMetricAssetTypeListComponent } from './admin-metric-asset-type-list.component';
import { AdminMetricConditionListComponent } from './admin-metric-condition-list.component';
import { AdminAnalyticsRoutingModule } from './admin-analytics.routes';
import { AdminMetricEditorComponent } from './admin-metric-editor.component';
import { AdminMetricListComponent } from './admin-metric-list.component';

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
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";
import { SiteModalModule } from '../../shared/modal/gov-modal.module';
import { AdminAllocationEditorComponent } from './admin-allocation-editor.component';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { AdminAnalyticsDetailsComponent } from './admin-metric-details.component';
import { InfoTooltipModule } from '../../shared/tooltip/info-tooltip.component';
import { MenuModule } from 'primeng/menu';
import { SwitchModule } from '../../shared/controls/switch/switch';
import { AdminMetricHistoryComponent } from './admin-metric-history.component'
import { IgDateModule } from '../../shared/controls/date/date';
import { PropertyGroupModule } from '../../shared/controls/property-group/property-group.component';
import { IgNumberFieldModule } from '../../shared/controls/number-picker/number-input.component';
import { FieldConditionGridModule } from '../../shared/controls/field-condition-grid/field-condition-grid.module';
import { IgMessageBoxModule } from '../../shared/controls/message-box/message-box.module';
import { PopupMenuModule } from '../../shared/controls/popup-menu/popup-menu.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        HttpClientModule,
        ReactiveFormsModule,

        AdminAnalyticsRoutingModule,

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
        PopupMenuModule

    ],
    declarations: [
        AdminAnalyticsComponent,
        AdminMetricAssetTypeListComponent,
        AdminMetricConditionListComponent,
        AdminMetricEditorComponent,
        AdminMetricListComponent,
        AdminAllocationEditorComponent,
        AdminAnalyticsDetailsComponent,
        AdminMetricHistoryComponent
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true
        }
    ]
})

export class AdminAnalyticsModule {
}
