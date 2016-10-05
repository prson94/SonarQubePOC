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
    ClaimsMatrixPart,    
    MenuPart,
    SimpleDropdown
} from './index';

@NgModule({
    declarations: [
        ActionBar,
        ClaimsMatrixPart,        
        MenuPart,        
        SimpleDropdown
    ],
    exports: [
        ActionBar,
        ClaimsMatrixPart,        
        MenuPart,        
        SimpleDropdown
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