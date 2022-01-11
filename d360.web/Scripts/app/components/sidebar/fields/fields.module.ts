import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';



import { RouterModule } from '@angular/router';

import { ButtonModule } from 'primeng/button';

import { CoreModule } from '../../shared/core.module';
import { TilesModule } from '../../shared/tiles/tiles.module';
import { FieldsRoutingModule } from './fields.routes';

import { FieldDefinitionComponent } from './field-definition.component';
import { SharedFieldDefinitionModule } from '../../shared/fielddefinition/shared-field-definition.module';


@NgModule({
    imports: [
        CommonModule,
        FormsModule,

        RouterModule,

        //routing 
        FieldsRoutingModule,

        //prime
        ButtonModule,

        //d3s        
        CoreModule,
        TilesModule,
        SharedFieldDefinitionModule,
        
    ],
    declarations: [
        FieldDefinitionComponent,
    ],
    providers: [

    ]
})
export class FieldsModule { }