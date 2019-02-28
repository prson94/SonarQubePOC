import {NgModule} from '@angular/core';
import {CommonModule, DeprecatedI18NPipesModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {HttpModule, XHRBackend} from '@angular/http';
import {HttpClientModule, HTTP_INTERCEPTORS} from '@angular/common/http';

import {AuthenticationConnectionBackend} from '../../../authentication-connection-backend';

import {CoreModule} from '../../shared/core.module';
import {PipesModule} from '../../../pipes/pipes.module';
import {TilesModule} from '../../shared/tiles/tiles.module';
import {SharedGridPagingInfoModule} from '../../shared/grid-paging-info.component';
import {SharedDeleteFormModule} from '../../shared/delete.form';
import {SharedDynamicGridEditorModule} from '../../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import {SharedObjectDetailsModule} from '../../shared/objectdetails/shared-object-details.module';

import {AdminAnalyticsComponent} from './admin-analytics.component';
import {AdminMetricAssetTypeListComponent} from './admin-metric-asset-type-list.component';
import {AdminMetricConditionListComponent} from './admin-metric-condition-list.component';
import {AdminMetricConditionEditorComponent} from './admin-metric-condition-editor.component';
import {AdminAnalyticsRoutingModule} from './admin-analytics.routes';
import {AdminMetricEditorComponent} from './admin-metric-editor.component';
import {AdminMetricListComponent} from './admin-metric-list.component';

import {
    ButtonModule,
    CalendarModule,
    DropdownModule,
    EditorModule,
    CheckboxModule,
    InputTextModule,
    MultiSelectModule,
    RadioButtonModule,
    SharedModule,
    SpinnerModule,
    TreeTableModule,
    TooltipModule,
} from 'primeng/primeng';
import {InputTextareaModule} from 'primeng/inputtextarea';
import {TableModule} from 'primeng/table';
import {SimpleAccordionModule} from '../../shared/simple-accordion.part';
import {ErrorNotifyInterceptor} from '../../../http-interceptors/error-notify-interceptor';
import {DirectivesModule} from '../../../directives/directives.module';
import {GovernHeadersInterceptor} from "../../../http-interceptors/govern-headers.interceptor";

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpModule,
        HttpClientModule,

        AdminAnalyticsRoutingModule,

        //prime
        ButtonModule,
        CalendarModule,
        DropdownModule,
        EditorModule,
        CheckboxModule,
        InputTextModule,
        InputTextareaModule,
        MultiSelectModule,
        RadioButtonModule,
        SharedModule,
        SpinnerModule,
        TreeTableModule,
        TableModule,

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
    ],
    declarations: [
        AdminAnalyticsComponent,
        AdminMetricAssetTypeListComponent,
        AdminMetricConditionListComponent,
        AdminMetricConditionEditorComponent,
        AdminMetricEditorComponent,
        AdminMetricListComponent
    ],
    providers: [
        {
            provide: XHRBackend,
            useClass: AuthenticationConnectionBackend
        },
        {
            provide: HTTP_INTERCEPTORS,
            useClass: ErrorNotifyInterceptor,
            multi: true
        },
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernHeadersInterceptor,
            multi: true
        }
    ]
})

export class AdminAnalyticsModule {
}
