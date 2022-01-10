import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';


import { SharedModule } from 'primeng/api';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { DropdownModule } from 'primeng/dropdown';
import { SpinnerModule } from 'primeng/spinner';
import { CheckboxModule } from 'primeng/checkbox';
import { MultiSelectModule } from 'primeng/multiselect';
import { TreeTableModule } from 'primeng/treetable';
import { EditorModule } from 'primeng/editor';

import { AssetTypeDeleteComponent } from './asset-type-delete.component';


@NgModule({
    imports: [
        CommonModule,
        FormsModule,



        //prime
        ButtonModule,
        CheckboxModule,
        DropdownModule,
        SpinnerModule,
        EditorModule,
        InputTextModule,
        MultiSelectModule,
        SharedModule,
        TreeTableModule,
    ],
    declarations: [
        AssetTypeDeleteComponent
    ],
    exports: [
        AssetTypeDeleteComponent
    ],
    providers: [
    ]
})

export class AssetTypeDeleteModule {
}
