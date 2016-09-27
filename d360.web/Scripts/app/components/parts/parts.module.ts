

import {  NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';

//import * as parts from './index';

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
    SimpleAccordion,
    SimpleDropdown
} from './index';

@NgModule({
    declarations: [
        ActionBar,
        ClaimsMatrixPart,        
        MenuPart,
        SimpleAccordion,
        SimpleDropdown
    ],
    exports: [
        ActionBar,
        ClaimsMatrixPart,        
        MenuPart,
        SimpleAccordion,
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