import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { CoreModule } from '../shared/core.module';
import { D3SSharedModule } from '../shared/shared.module';
import { PipesModule } from '../../pipes/pipes.module';

import { ReferenceComponent } from './reference.component';
import { ReferenceListComponent } from './reference-list.component';
import { ReferenceItemTypeEditorComponent } from './reference-item-type-editor.component';
import { ReferenceItemTypeGridComponent } from './reference-item-type-list.component';


import {
    ButtonModule,
    GrowlModule,
    DataTableModule,
    EditorModule,
    SharedModule,

} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,

        //primeng
        ButtonModule,
        GrowlModule,
        DataTableModule,
        EditorModule,
        SharedModule,


        //d3s
        D3SSharedModule,
        CoreModule,
        PipesModule,
    ],
    declarations: [
        ReferenceComponent,
        ReferenceItemTypeEditorComponent,
        ReferenceItemTypeGridComponent,
        ReferenceListComponent,
    ]    
})
export class ReferenceModule { }