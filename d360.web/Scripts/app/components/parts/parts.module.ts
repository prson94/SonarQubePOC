import {  NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';

import {
    TreeTableModule,
    DataTableModule,
    InputTextModule,
    InputMaskModule,
    ButtonModule,
    EditorModule,
    DropdownModule,
    MultiSelectModule,
    SpinnerModule,
} from 'primeng/primeng';

import {
    ActionBar,    
    MenuPart    
} from './index';

@NgModule({
    declarations: [
        ActionBar,        
        MenuPart
    ],
    exports: [
        ActionBar,        
        MenuPart
        ]
    , imports: [
        TreeTableModule,
        DataTableModule,
        InputTextModule,
        InputMaskModule,
        ButtonModule,
        EditorModule,
        DropdownModule,
        MultiSelectModule,
        SpinnerModule,
        BrowserModule,
        FormsModule,
    ]

})

export class PartsModule { }