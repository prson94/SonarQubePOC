import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';


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
import { SharedDeleteFormModule } from '../delete.form';
import { AssetDeleteEditorComponent } from './asset-delete-editor.component';

@NgModule({
    imports: [CommonModule,

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
        
    ]
})
export class SharedAssetEditorsModule { }