import { NgModule } from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HttpModule, XHRBackend } from '@angular/http';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

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
        HttpModule,
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
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class SharedAssetTypeEditorModule { }