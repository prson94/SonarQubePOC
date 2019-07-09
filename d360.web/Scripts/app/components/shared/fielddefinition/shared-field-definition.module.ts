import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";

import {
    CalendarModule,
    ButtonModule,
    DropdownModule,
    InputTextModule,
    InputTextareaModule,
    EditorModule,
    MultiSelectModule,    
    SharedModule,
    CheckboxModule
} from 'primeng/primeng';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { TableModule } from 'primeng/table';

import { CoreModule } from '../core.module';
import { TilesModule  } from '../tiles/tiles.module';
import { SharedDeleteFormModule } from '../delete.form';
import { SharedGridPagingInfoModule } from '../grid-paging-info.component';
import { SharedFormMessageModule } from '../form-message.part'
import { SimpleAccordionModule } from '../simple-accordion.part';

import { FieldTypeForm } from './field-type-form/field-type.form';
import { FieldDefinitionComponent } from './field-definition.component';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,
        //d3s
        CoreModule,
        SharedDeleteFormModule,
        SharedFormMessageModule,
        SharedGridPagingInfoModule,
        TilesModule,
        SimpleAccordionModule,   

        //prime
        AutoCompleteModule,
        CalendarModule,
        ButtonModule,
        CheckboxModule,
        DropdownModule,
        InputTextModule,
        InputTextareaModule,
        EditorModule,
        MultiSelectModule,
        SharedModule,
        TableModule,
    ],
    declarations: [
        FieldTypeForm,
        FieldDefinitionComponent
    ],
    exports: [
        FieldDefinitionComponent
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
    ]
})
export class SharedFieldDefinitionModule { }
