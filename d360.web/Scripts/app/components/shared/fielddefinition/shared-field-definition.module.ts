import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

import {
    ButtonModule,
    DataTableModule,
    DropdownModule,
    InputTextModule,
    EditorModule,
    MultiSelectModule,    
    SharedModule,
    CheckboxModule
} from 'primeng/primeng';

import { CoreModule } from '../core.module';
import { TilesModule  } from '../tiles/tiles.module';
import { SharedDeleteFormModule } from '../delete.form';
import { SharedGridPagingInfoModule } from '../grid-paging-info.component';
import { SharedFormMessageModule } from '../form-message.part'


import { FieldTypeForm } from './field-type.form';
import { FieldDefinitionComponent } from './field-definition.component';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        //d3s
        CoreModule,
        SharedDeleteFormModule,
        SharedFormMessageModule,
        SharedGridPagingInfoModule,
        TilesModule,

        //prime
        ButtonModule,
        CheckboxModule,
        DataTableModule,
        DropdownModule,
        InputTextModule,
        EditorModule,
        MultiSelectModule,
        SharedModule,
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