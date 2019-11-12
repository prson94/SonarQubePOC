import { NgModule } from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

import { ColorPickerModule } from 'primeng/colorpicker';
import { SpinnerModule } from 'primeng/spinner';
import { ButtonModule } from 'primeng/button';
import { DropdownModule } from 'primeng/dropdown';
import { SharedModule } from 'primeng/shared';
import { InputTextModule } from 'primeng/inputtext';
import { EditorModule } from 'primeng/editor';

import { CoreModule } from '../core.module';
import { TilesModule } from '../tiles/tiles.module';

import { AssetTypeEditorComponent } from './asset-type-editor.component';
import { SimpleAccordionModule } from '../simple-accordion.part';
import { AssetTypeEditorUseAsTransformationComponent } from './asset-type-editor-use-as-transformation.component';
import { IconPickerModule } from '../icon-picker.component';
import { AssetTypeModalEditorComponent } from './asset-type-modal-editor';
import { SiteModalModule } from '../modal/gov-modal.module';
import { SharedDynamicGridEditorModule } from '../dynamicgrideditor/shared-dynamic-grid-editor.module';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
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
        SiteModalModule,
        //prime        
        ButtonModule,
        ColorPickerModule,
        DropdownModule,
        SpinnerModule,
        EditorModule,
        InputTextModule,
        SharedModule,
    ],
    declarations: [
        AssetTypeEditorComponent,
        AssetTypeModalEditorComponent,
        AssetTypeEditorUseAsTransformationComponent
    ],
    exports: [
        AssetTypeEditorComponent, AssetTypeModalEditorComponent
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
    ]
})
export class SharedAssetTypeEditorModule { }