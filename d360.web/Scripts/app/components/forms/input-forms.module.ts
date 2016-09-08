///<reference path="../../es6-shim.d.ts"/>
import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { PartsModule } from '../parts/parts.module';

import { BrowserModule } from '@angular/platform-browser';


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
    CalendarModule,
} from 'primeng/primeng';


import {
    ArtifactTypeForm,    
    FieldTypeForm,
    GroupForm,
    LoadForm,
    ResponsibilityItemForm,
    ResponsibilityTypeForm,
    WorkflowItemForm

} from './index';



//import { FormMessagePart } from '../parts/form-message.part'; 


@NgModule({
    declarations: [
        ArtifactTypeForm,        
        FieldTypeForm,
        GroupForm,
        LoadForm,
        ResponsibilityItemForm,
        ResponsibilityTypeForm,
        WorkflowItemForm,
        //FormMessagePart
    ]
    , imports: [
        CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,

        TreeTableModule,
        DataTableModule,
        InputTextModule,
        InputMaskModule,
        ButtonModule,
        EditorModule,
        DropdownModule,
        MultiSelectModule,
        SpinnerModule,
        CalendarModule,
        BrowserModule,        
        FormsModule,
        PartsModule
    ]

})

export class InputFormsModule { }