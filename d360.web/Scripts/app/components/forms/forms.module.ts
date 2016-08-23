///<reference path="../../es6-shim.d.ts"/>
import { FormsModule, ReactiveFormsModule }    from '@angular/forms';
import { NgModule } from '@angular/core';
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
    DeleteForm,
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
        DeleteForm,
        FieldTypeForm,
        GroupForm,
        LoadForm,
        ResponsibilityItemForm,
        ResponsibilityTypeForm,
        WorkflowItemForm,
        //FormMessagePart
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
        CalendarModule,
        BrowserModule,
        ReactiveFormsModule,
        FormsModule,
        PartsModule
    ]

})

export class FormModule { }