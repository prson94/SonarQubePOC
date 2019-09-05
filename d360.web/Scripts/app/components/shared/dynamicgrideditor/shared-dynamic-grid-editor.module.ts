import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { RouterModule } from '@angular/router';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";

import { FormsModule, ReactiveFormsModule } from '@angular/forms';

import {
    CalendarModule,
    ColorPickerModule,
    DropdownModule,
    EditorModule,
    MultiSelectModule,
    SharedModule,
    TooltipModule,
    AutoCompleteModule,
} from 'primeng/primeng';

import { TableModule } from 'primeng/table';

import { CoreModule } from '../core.module';
import { PipesModule } from '../../../pipes/pipes.module';
import { SharedDeleteFormModule } from '../delete.form';
import { SharedGridPagingInfoModule } from '../grid-paging-info.component';
import { SharedAssetEditorsModule } from '../asseteditors/shared-asset-editor.module';
import { TilesModule  } from '../tiles/tiles.module';
import { SimilarItemsModule } from '../similar-items.component';

import { DynamicEditorComponent } from './dynamic-editor.component';
import { DynamicFieldComponent } from './dynamic-field.component';
import { DynamicFieldValueComponent } from './dynamic-field-value.component';
import { DynamicGridComponent } from './dynamic-grid.component';
import { MultiSelectGridComponent } from './multiselect-grid.component';
import { SimpleAccordionModule } from '../simple-accordion.part';
import { DirectivesModule } from '../../../directives/directives.module';
import { NgxJsonViewModule } from 'ng-json-view';
import { SiteModalModule } from '../modal/gov-modal.module';
import { TagUsageInfoModule } from '../../admin/tags/tags-usage-info.module';
import { TagView } from "../../shared/tags/d3s-tag-view";


@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        HttpClientModule,
        ReactiveFormsModule,
        FormsModule,
        RouterModule,

        //d3s
        CoreModule,
        SharedDeleteFormModule,
        SharedGridPagingInfoModule,
        SharedAssetEditorsModule,
        TilesModule,
        SimpleAccordionModule,
        SimilarItemsModule,
        TagUsageInfoModule,

        //prime        
        CalendarModule,
        DropdownModule,
        EditorModule,
        MultiSelectModule,
        PipesModule,
        SharedModule,
        TooltipModule,
        AutoCompleteModule,
        TableModule,
        DirectivesModule,
        SiteModalModule,

        //color picker
        ColorPickerModule,

        //JSON Viewer module
        NgxJsonViewModule,

    ],
    declarations: [
        TagView,
        DynamicEditorComponent,
        DynamicFieldComponent,
        DynamicFieldValueComponent,
        DynamicGridComponent,
        MultiSelectGridComponent,
    ],
    exports: [
        TagView,
        DynamicEditorComponent,
        DynamicFieldValueComponent,
        DynamicGridComponent,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
    ]
})
export class SharedDynamicGridEditorModule { }