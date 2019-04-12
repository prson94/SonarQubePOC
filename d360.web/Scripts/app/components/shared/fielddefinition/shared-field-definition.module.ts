import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

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
        HttpModule,
        //d3s
        CoreModule,
        SharedDeleteFormModule,
        SharedFormMessageModule,
        SharedGridPagingInfoModule,
        TilesModule,
        SimpleAccordionModule,   

        //prime
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
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class SharedFieldDefinitionModule { }
