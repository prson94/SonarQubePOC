import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { RouterModule } from '@angular/router';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";

import { FormsModule, ReactiveFormsModule } from '@angular/forms';

import { AutoCompleteModule } from 'primeng/autocomplete';
import { CalendarModule } from 'primeng/calendar';
import { DropdownModule } from 'primeng/dropdown';
import { SharedModule } from 'primeng/api';
import { TooltipModule } from 'primeng/tooltip';
import { EditorModule } from 'primeng/editor';
import { OverlayPanelModule } from 'primeng/overlaypanel';
import { MultiSelectModule } from 'primeng/multiselect';
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
import { TagViewModule } from '../tags/d3s-tag-view.module';
import { IgColorPickerModule } from '../controls/color-picker/color-picker.module';
import { SwitchModule } from '../controls/switch/switch';
import { IgDateModule } from '../controls/date/date';
import { IgNumberFieldModule } from '../controls/number-picker/number-input.component';
import { OwnershipListModule } from "../small-widgets/ownership-list/ownership-list.component";
import { AssetEditorComponent } from './asset-editor.component';
import { RadioButtonModule } from 'primeng/radiobutton';

@NgModule({
    imports: [
        CommonModule,
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
        TagViewModule,
        IgColorPickerModule,
        SwitchModule,
        IgDateModule,
        IgNumberFieldModule,
        OwnershipListModule,
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
        OverlayPanelModule,
        RadioButtonModule,

        NgxJsonViewModule
    ],
    declarations: [
        AssetEditorComponent,
        DynamicEditorComponent,
        DynamicFieldComponent,
        DynamicFieldValueComponent,
        DynamicGridComponent,
        MultiSelectGridComponent,
    ],
    exports: [
        AssetEditorComponent,
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