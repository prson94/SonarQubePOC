import { NgModule } from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernPostRequestInterceptor } from "../../../http-interceptors/govern-post-request.interceptor";
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

import {
    ButtonModule,
    ColorPickerModule,
    DropdownModule,
    SpinnerModule,
    EditorModule,
    InputTextModule,
    SharedModule,
} from 'primeng/primeng';

import { CoreModule } from '../core.module';
import { TilesModule } from '../tiles/tiles.module';

import { AssetTypeEditorComponent } from './asset-type-editor.component';
import { SimpleAccordionModule } from '../simple-accordion.part';

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
        SimpleAccordionModule,        

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
        AssetTypeEditorComponent
    ],
    exports: [
        AssetTypeEditorComponent
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernPostRequestInterceptor,
            multi: true },
    ]
})
export class SharedAssetTypeEditorModule { }