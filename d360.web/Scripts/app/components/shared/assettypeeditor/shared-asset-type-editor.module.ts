import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

import { ColorPickerModule } from 'primeng/colorpicker';
import { SpinnerModule } from 'primeng/spinner';
import { ButtonModule } from 'primeng/button';
import { DropdownModule } from 'primeng/dropdown';
import { SharedModule } from 'primeng/api';
import { InputTextModule } from 'primeng/inputtext';
import { EditorModule } from 'primeng/editor';

import { CoreModule } from '../core.module';
import { TilesModule } from '../tiles/tiles.module';

import { AssetTypeEditorComponent } from './asset-type-editor.component';
import { SimpleAccordionModule } from '../simple-accordion.part';
import { AssetTypeEditorUseAsTransformationComponent } from './asset-type-editor-use-as-transformation.component';
import { IconPickerModule } from '../controls/icon-picker/icon-picker.component';
import { SharedDynamicGridEditorModule } from '../dynamicgrideditor/shared-dynamic-grid-editor.module';
import { InfoTooltipModule } from '../tooltip/info-tooltip.component';
import { CheckboxModule } from 'primeng/checkbox';
import { DirectivesModule } from '../directives/directives.module';

@NgModule({
    imports: [CommonModule,
        HttpClientModule,
        ReactiveFormsModule,
        FormsModule,
        RouterModule,
        SharedDynamicGridEditorModule,
        //d3s
        CoreModule,                
        TilesModule,
        SimpleAccordionModule,        
        IconPickerModule,
        InfoTooltipModule,
        DirectivesModule,
        //prime        
        ButtonModule,
        ColorPickerModule,
        DropdownModule,
        SpinnerModule,
        EditorModule,
        InputTextModule,
        SharedModule,
        CheckboxModule
    ],
    declarations: [
        AssetTypeEditorComponent,
        AssetTypeEditorUseAsTransformationComponent
    ],
    exports: [
        AssetTypeEditorComponent
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
    ]
})
export class SharedAssetTypeEditorModule { }