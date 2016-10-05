import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { CoreModule } from '../shared/core.module';
import { SharedModule } from '../shared/shared.module';
import { PipesModule } from '../../pipes/pipes.module';

import { ReferenceComponent } from './reference.component';
import { ReferenceListComponent } from './reference-list.component';
import { ReferenceItemTypeGridComponent } from './reference-item-type-list.component';

import {
    GrowlModule,
    DataTableModule,
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,

        //primeng
        GrowlModule,
        DataTableModule,


        //d3s
        SharedModule,
        CoreModule,
        PipesModule,
    ],
    declarations: [
        ReferenceComponent,
        ReferenceItemTypeGridComponent,
        ReferenceListComponent,
    ],
    exports: [
        ReferenceComponent,
        ReferenceItemTypeGridComponent,
        ReferenceListComponent,
    ]
})
export class ReferenceModule { }