import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';



import { ButtonModule } from 'primeng/button';
import { SharedModule } from 'primeng/api';
import { InputTextModule } from 'primeng/inputtext';
import { DropdownModule } from 'primeng/dropdown';
import { TableModule } from 'primeng/table';
import { EditorModule } from 'primeng/editor';
import { MultiSelectModule } from 'primeng/multiselect';
import { CheckboxModule } from 'primeng/checkbox';
import { TooltipModule } from "primeng/tooltip";

import { CoreModule } from '../core.module';
import { TilesModule  } from '../tiles/tiles.module';
import { SharedDeleteFormModule } from '../delete.form';
import { SharedFormMessageModule } from '../form-message.part';
import { SharedGridPagingInfoModule } from '../grid-paging-info.component';

import { PeopleResponsibilitiesModule } from './people-responsibilities.tile';
import { D3SSharedModule } from '../shared.module';
import { ResponsibilityRelationsComponent } from './responsibility-relations.component';
import { ResponsibilityRelationForm } from './responsibility-relation.form';
import { PipesModule } from '../../../pipes/pipes.module';
import { ResourceMultiSelectGridModule } from '../resource-multiselect-grid.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,


        //d3s
        CoreModule,
        TilesModule,
        SharedDeleteFormModule,
        SharedFormMessageModule,
        SharedGridPagingInfoModule,
        D3SSharedModule,
        PipesModule,
        ResourceMultiSelectGridModule,
        PeopleResponsibilitiesModule,

        //prime
        ButtonModule,
        CheckboxModule,
        DropdownModule,
        InputTextModule,
        EditorModule,
        MultiSelectModule,
        SharedModule,
        TableModule,
        TooltipModule,
    ],
    declarations: [
        ResponsibilityRelationsComponent,
        ResponsibilityRelationForm
    ],
    exports: [
        ResponsibilityRelationsComponent,
        ResponsibilityRelationForm
    ],
    providers: [

    ]
})
export class SharedResponsibilitiesModule { }