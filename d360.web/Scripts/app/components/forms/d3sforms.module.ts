import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule }     from '@angular/http';
import { RouterModule } from '@angular/router';


import { FieldTypeForm } from './field-type.form';
import { ResponsibilityItemForm } from './responsibility-item.form';
import { FormMessagePart } from './form-message.part';

import { PipesModule } from '../../pipes/pipes.module';

import { ColorPickerModule } from 'angular2-color-picker';


import {
    GrowlModule,
    InputTextModule,
    InputMaskModule,
    DataTableModule,
    TreeTableModule,
    ButtonModule,
    DropdownModule,
    CheckboxModule,
    CalendarModule,    
    MenuModule,
    MenubarModule,
    AccordionModule,
    SelectButtonModule,
    AutoCompleteModule,
    MultiSelectModule,
    SpinnerModule,    
    TooltipModule,    
    PaginatorModule,
    EditorModule,
    SharedModule,  
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,

        //primeng
        GrowlModule,
        InputTextModule,
        InputMaskModule,
        DataTableModule,
        TreeTableModule,
        ButtonModule,
        DropdownModule,
        CheckboxModule,
        CalendarModule,
        MenuModule,
        MenubarModule,
        AccordionModule,
        SelectButtonModule,
        AutoCompleteModule,
        MultiSelectModule,
        SpinnerModule,        
        TooltipModule,                
        PaginatorModule,
        EditorModule,
        SharedModule,

        ColorPickerModule,

        //d3s          
        PipesModule,
    ],
    declarations: [        
        FieldTypeForm,
        FormMessagePart,           
        ResponsibilityItemForm,        
    ],
    exports: [        
        FieldTypeForm,
        FormMessagePart,        
        ResponsibilityItemForm,        
    ]
})
export class D3SFormsModule { }