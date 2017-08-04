import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { RouterModule } from '@angular/router';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

import {
    CalendarModule,
    ColorPickerModule,
    DataTableModule,
    EditorModule,
    MultiSelectModule,
    SharedModule,
    TooltipModule,
} from 'primeng/primeng';

//import { ColorPickerModule } from 'ngx-color-picker';

import { CoreModule } from '../core.module';
import { PipesModule } from '../../../pipes/pipes.module';
import { SharedDeleteFormModule } from '../delete.form';
import { SharedGridPagingInfoModule } from '../grid-paging-info.component';
import { TilesModule  } from '../tiles/tiles.module';
import { SimilarItemsModule } from '../similar-items.component';

import { DynamicEditorComponent } from './dynamic-editor.component';
import { DynamicFieldComponent } from './dynamic-field.component';
import { DynamicFieldValueComponent } from './dynamic-field-value.component';
import { DynamicGridComponent } from './dynamic-grid.component';
import { MultiSelectGridComponent } from './multiselect-grid.component';
import { SimpleAccordionModule } from '../simple-accordion.part';

@NgModule({
    imports: [CommonModule,
        HttpModule,
        ReactiveFormsModule,
        FormsModule,
        RouterModule,
        //d3s
        CoreModule,
        SharedDeleteFormModule,
        SharedGridPagingInfoModule,
        TilesModule,
        SimpleAccordionModule,
        SimilarItemsModule,

        //prime        
        CalendarModule,
        DataTableModule,
        EditorModule,
        MultiSelectModule,
        PipesModule,
        SharedModule,
        TooltipModule,

        //color picker
        ColorPickerModule,
    ],
    declarations: [
        DynamicEditorComponent,
        DynamicFieldComponent,
        DynamicFieldValueComponent,
        DynamicGridComponent,
        MultiSelectGridComponent,
    ],
    exports: [
        DynamicEditorComponent,
        DynamicFieldValueComponent,
        DynamicGridComponent,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class SharedDynamicGridEditorModule { }