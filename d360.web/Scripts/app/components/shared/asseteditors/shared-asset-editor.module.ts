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
import { SharedDeleteFormModule } from '../delete.form';

import { AssetDeleteEditorComponent } from './asset-delete-editor.component';
import { SharedDynamicGridEditorModule } from '../dynamicgrideditor/shared-dynamic-grid-editor.module';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        HttpClientModule,
        ReactiveFormsModule,
        FormsModule,
        RouterModule,
        //d3s
        CoreModule,
        TilesModule,
        SharedDeleteFormModule,

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
        AssetDeleteEditorComponent
    ],
    exports: [
        AssetDeleteEditorComponent
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true
        },
    ]
})
export class SharedAssetEditorsModule { }