import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';



import { SharedModule } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DropdownModule } from 'primeng/dropdown';
import { InputTextModule } from 'primeng/inputtext';
import { MultiSelectModule } from "primeng/multiselect";

import { TableModule } from 'primeng/table';

import { CoreModule } from '../../shared/core.module';
import { TilesModule  } from '../../shared/tiles/tiles.module';

import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../../shared/delete.form';
import { SharedDynamicGridEditorModule } from '../../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedFieldDefinitionModule } from '../../shared/fielddefinition/shared-field-definition.module';

import { AdminIssueTypesComponent } from './admin-issue-types.component';
import { AdminIssueTypeAllocationComponent } from './admin-issue-type-allocation.component';
import { AdminIssueTypeAllocationEditorComponent } from "./admin-issue-type-allocation-editor.component";


import { AdminIssueTypesRoutingModule } from './admin-issue-types.routes';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,


        AdminIssueTypesRoutingModule,

        //prime
        ButtonModule,
        DropdownModule,
        InputTextModule,
        SharedModule,
        TableModule,
        MultiSelectModule,

        //d3s                
        CoreModule,
        
        SharedDeleteFormModule,
        SharedDynamicGridEditorModule,     
        SharedFieldDefinitionModule,   
        SharedGridPagingInfoModule,
        TilesModule,
    ],
    declarations: [
        AdminIssueTypesComponent,
        AdminIssueTypeAllocationComponent,
        AdminIssueTypeAllocationEditorComponent
    ],
    providers: [
    ]
})
export class AdminIssueTypesModule { }